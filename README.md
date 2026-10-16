# Agentic Orchestrator

A web platform where a team of AI agents picks up a software ticket, works out what needs to change, writes the code and reviews it. A human still has the final say before anything turns into a Pull Request.

This is our student project at ENSEIRB-MATMECA for 2026–2027. The subject was proposed by [Avanade](https://www.avanade.com).

> **Where we are:** we're still in the scoping phase (October 2026), so there's no code here yet. This README will grow along with the project.

## Why we're building this

Handling a ticket usually goes the same way. You read it and figure out what's actually being asked, plan the changes, write the code, then check that everything still holds. It's valuable work, but it's slow, and a lot of it looks the same from one ticket to the next.

We want to see how much of that loop a group of specialized AI agents can take on, without the team losing control of what ends up in the codebase.

## How it works

You pick a ticket from a GitHub or Azure DevOps backlog and start a run. Four agents then handle it one after another:

- an **analyst** clarifies the need and flags anything missing;
- a **planner** breaks the work into tasks and looks at the technical impact;
- a **developer** writes the code on a dedicated branch;
- a **reviewer** checks the result against the acceptance criteria.

The run then stops and waits for you. You look over the work in the portal, and only once you approve it does the platform open a Pull Request linked to the original ticket. Every step is recorded (which agent ran it, when, what went in and what came out), so you can always trace how a change was produced.

## Tech stack

- **Frontend:** Nuxt (Vue.js)
- **Backend:** ASP.NET Core REST API
- **Agents:** Microsoft Agent Framework, with models served through Microsoft Foundry
- **Data:** Azure SQL Database
- **Messaging:** Azure Service Bus, so long-running agent work doesn't block the API
- **Hosting:** Azure Container Apps and Container Apps Jobs
- **Local orchestration and observability:** .NET Aspire with OpenTelemetry
- **Infrastructure as code:** Pulumi
- **CI/CD:** GitHub Actions
- **Authentication:** Entra ID or GitHub OAuth, with Developer and Product Owner roles

## A few ground rules

- **Agents never merge anything.** They work on their own branch, the main branch is protected, and no PR is opened without a human's approval.
- **No secrets in the repo.** They live in Azure Key Vault or Container Apps secrets, and in `user-secrets` when running locally.
- **Failures don't throw work away.** Each step's output is saved, external calls are retried, and a failed run can be restarted from the portal.
- **Adding to it should be easy.** Agents and ticket sources sit behind shared interfaces, so a new one doesn't mean a rewrite.
- **Costs stay in check.** Each run has a token budget, and Azure spending has alerts set up.

## Running it locally

The goal is to start the whole platform with a single Aspire command. We'll add the exact steps here once the project skeleton is in place.

## What this project won't do

- It won't replace GitHub or Azure DevOps. It plugs into them.
- It won't merge or deploy code without someone signing off.
- It won't promise to solve every ticket on its own. It's there to help, not to take over.
- It won't train its own AI model. We use the models available through Foundry.

## Roadmap

| When | What |
|---|---|
| October 2026 | Scoping, requirements document, repository setup |
| October – November 2026 | Aspire, infrastructure as code, CI/CD, first frontend-to-backend endpoint |
| November – early December 2026 | Ticket integration and the multi-agent workflow |
| Early December 2026 | Intermediate delivery |
| December 2026 – early January 2027 | Testing, fixes, documentation, deployment |
| Early January 2027 | Production release |
| Late January 2027 | Final presentation and demo |

We work in two-week sprints, track the backlog in GitHub Projects (under the *Mentoring-Bordeaux* organization), and meet with Avanade every week.

## Team

**Students:** Mohamed Amine Benkadur, Yasmine Benayed, Hafsa Hounaoui, Ibtihel Manaa, Aroua Fessi

**School supervisor:** Mohamed Mosbah

**Avanade mentors:** Théo Gomichon, Faustin Boitel, Alexandre Nédélec
