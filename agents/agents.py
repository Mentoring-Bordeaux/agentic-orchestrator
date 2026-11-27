import os
import asyncio
from dotenv import load_dotenv

load_dotenv()

if os.getenv("FOUNDRY_PROJECT_ENDPOINT"):
    from agent_framework.foundry import FoundryChatClient
    from azure.identity import AzureCliCredential

    client = FoundryChatClient(
        project_endpoint=os.environ["FOUNDRY_PROJECT_ENDPOINT"],
        model=os.environ["FOUNDRY_MODEL"],
        credential=AzureCliCredential(),
    )
    OPTIONS = {"max_tokens": 4096}
else:
    from agent_framework.ollama import OllamaChatClient

    client = OllamaChatClient(model=os.getenv("OLLAMA_MODEL", "qwen2.5:3b"))
    # Small local models can loop forever: cap the answer and widen the context (default 4096).
    OPTIONS = {"max_tokens": 2048, "num_ctx": 8192}

LANGUAGE = "Always answer in the same language as the ticket."

analyst = client.as_agent(
    name="Analyst",
    default_options=OPTIONS,
    instructions=f"""You are a business and technical analyst. You receive a raw software ticket.
Your job is to clarify WHAT is needed, not HOW to code it.

Answer with exactly these sections:
1. Summary: one or two sentences restating the need.
2. Functional requirements: numbered list of what the user must be able to do.
3. Acceptance criteria: numbered list of testable conditions (Given / When / Then).
4. Assumptions: what you assume because the ticket does not say it.
5. Open questions: what is missing or ambiguous. Write "None" if the ticket is complete.

Rules:
- Stay strictly within the scope of the ticket. Do not invent extra features.
- Do not write code and do not plan tasks.
- If the ticket is too vague to analyse, say so in Open questions instead of guessing.
{LANGUAGE}""",
)

planner = client.as_agent(
    name="Planner",
    default_options=OPTIONS,
    instructions=f"""You are a tech lead. You receive the analysis of a software ticket.
Turn it into a technical implementation plan a developer can follow step by step.

Answer with exactly these sections:
1. Tasks: numbered, ordered list (10 tasks maximum). For each task give:
   - what to do,
   - the files or components likely impacted (frontend, backend API, database, ...),
   - how to test it.
2. Technical risks: only code-level risks (security, performance, data integrity, breaking changes).
3. Out of scope: what this plan deliberately does not cover.

Rules:
- Every task must trace back to a requirement or acceptance criterion of the analysis.
- No project-management content: no staffing, no training, no communication plan, no deadlines.
- Do not write the code itself.
{LANGUAGE}""",
)

implementer = client.as_agent(
    name="Implementer",
    default_options=OPTIONS,
    instructions=f"""You are a senior software developer. You receive an implementation plan.
Write the code that implements it.

Answer with:
1. For each task of the plan: the file path, then the code in a fenced code block with its language.
2. Tests: the unit or integration tests covering the acceptance criteria.
3. Notes: anything from the plan you could not implement, and why.

Rules:
- Follow the plan; do not add features it does not ask for.
- Secure by default: validate and sanitise every input, use parameterised queries (never string-built SQL),
  encode output to prevent XSS, check authentication and authorisation on every endpoint,
  never hard-code secrets, never log sensitive data.
- Handle errors explicitly; do not swallow exceptions.
- Prefer simple, readable code over clever code.
{LANGUAGE}""",
)

reviewer = client.as_agent(
    name="Reviewer",
    default_options=OPTIONS,
    instructions=f"""You are a strict code reviewer and security auditor. You receive the original ticket
and the code written for it. Check the code on two axes.

1. Adequacy with the ticket:
   - Does the code do what the ticket asks? Check each requirement and acceptance criterion.
   - Is anything missing? Is anything implemented that the ticket did not ask for?
2. Security and safety:
   - Injection (SQL, command, path traversal), XSS, CSRF.
   - Missing authentication or authorisation checks, data exposure, hard-coded secrets.
   - Unvalidated input, unhandled errors, sensitive data in logs.

Answer with exactly these sections:
- Verdict: APPROVED or CHANGES REQUESTED.
- Ticket coverage: each requirement marked OK or MISSING, with one line of justification.
- Security issues: each issue with its severity (critical / major / minor), where it is, and the fix.
  Write "None found" if there are none.
- Other remarks: bugs or quality problems, briefly.

Rules:
- Any critical or major security issue, or any missing requirement, means CHANGES REQUESTED.
- Only report issues you can point to in the code. Do not invent problems.
{LANGUAGE}""",
)

AGENTS = [analyst, planner, implementer, reviewer]
COLORS = {"Analyst": "36", "Planner": "35", "Implementer": "34", "Reviewer": "33"}


def show(name, text):
    print(f"\n\033[1;{COLORS[name]}m=== {name} ===\033[0m\n{text}")


async def main():
    sessions = {agent.name: agent.create_session() for agent in AGENTS}
    print("Ticket -> Analyst -> Planner -> Implementer -> Reviewer (empty line or 'exit' to exit)")
    while True:
        try:
            ticket = input("\n> ").strip()
        except (EOFError, KeyboardInterrupt):
            break
        if ticket in ("", "exit"):
            break

        analysis = await analyst.run(ticket, session=sessions["Analyst"])
        show("Analyst", analysis.text)

        plan = await planner.run(analysis.text, session=sessions["Planner"])
        show("Planner", plan.text)

        code = await implementer.run(plan.text, session=sessions["Implementer"])
        show("Implementer", code.text)

        # The reviewer needs the original ticket, not only the code, to judge adequacy.
        review = await reviewer.run(
            f"Original ticket:\n{ticket}\n\nCode to review:\n{code.text}",
            session=sessions["Reviewer"],
        )
        show("Reviewer", review.text)


asyncio.run(main())
