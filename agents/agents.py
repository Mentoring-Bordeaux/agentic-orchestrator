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
else:
    from agent_framework.ollama import OllamaChatClient

    client = OllamaChatClient(model=os.getenv("OLLAMA_MODEL", "qwen2.5:3b"))

analyst = client.as_agent(
    name="Analyst",
    instructions="You analyse the developer's ticket, returns a detailed instruction list of what should be done and how.",
)

planner = client.as_agent(
    name="Planner",
    instructions=(
        "You receive the analysis of a software ticket written by an analyst. "
        "Turn it into a detailed implementation plan: ordered tasks, files or components "
        "likely impacted, technical risks, and how to test each task."
    ),
)


async def main():
    analyst_session = analyst.create_session()
    planner_session = planner.create_session()
    print("Ticket -> Analyst -> Planner (empty line or 'exit' to exit)")
    while True:
        try:
            message = input("\n> ").strip()
        except (EOFError, KeyboardInterrupt):
            break
        if message in ("", "exit"):
            break
        analysis = await analyst.run(message, session=analyst_session)
        print(f"\n\033[1;36m=== Analyst ===\033[0m\n{analysis.text}")
        plan = await planner.run(analysis.text, session=planner_session)
        print(f"\n\033[1;35m=== Planner ===\033[0m\n{plan.text}")


asyncio.run(main())
