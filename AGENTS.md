# PBJ Project Instructions

## Project
- Unity 6000.3.23f1
- Netcode for GameObjects 2.7.0
- Unity Transport 2.7.4
- Multiplayer Play Mode 2.0.2
- 4-player cooperative Pick-up and Deliver
- 1 Host/Server + 3 Clients
- Listen Server
- LAN / Direct IP
- Server-authoritative architecture

## Ownership
- Abim: Project Manager and Lead Engineer for Systems/Networking/Infrastructure.
- Gameplay agent: player gameplay, movement, interaction, pickup/carry/drop, Shaman, Offerings, gameplay-side integration.
- Systems-network agent: NGO, NetworkManager, NetworkSession, Unity Transport, LAN/session infrastructure, authoritative network state.
- Verify agent: read-only QA, code review, integration verification.
- Artists own art assets and presentation.
- Gameplay must not create a parallel networking architecture.
- Systems-network must not redesign gameplay owned by gameplay.

## Source of Truth
- Git is the source of truth for shared production work.
- Assets/_Project is production.
- Assets/Playground/<name> is experimentation/prototyping.
- Prototype -> Test -> Integrate -> Verify -> Production.
- Reuse existing systems before creating new ones.
- Inspect before modifying.
- Do not invent requirements or unresolved design decisions.

## Unity Safety
- Do not manually manufacture Unity fileIDs, GlobalObjectIds, hashes, or serialized scene data.
- Prefer code changes over Unity scene YAML changes.
- Do not modify shared scenes unless explicitly assigned.
- Do not modify SampleScene unless explicitly assigned.
- The human developer handles Unity Editor scene/prefab hierarchy, Inspector configuration, Button events, Sprite Editor, Animator setup, Play Mode, and MPPM runtime operation.
- Agents may inspect and verify Unity state with available CLI/static tooling.
- Never claim Play Mode, MPPM, LAN, or runtime success without actual runtime evidence.

## Networking Safety
- Exactly one NetworkManager.
- Do not create a duplicate NetworkSession.
- Reuse established networking/session APIs.
- Gameplay networking requirements must go through the existing networking architecture.
- Server validates authoritative gameplay decisions.

## Git Safety
- Inspect git status, branch, and recent history before modifying files.
- Never use reset --hard, force push, or destructive cleanup unless explicitly instructed by Abim.
- Never use git add . blindly.
- Do not delete or overwrite another person's work.
- Keep commits small and logically scoped.
- Agents must not commit or push unless explicitly instructed by Abim.
- Never merge work directly into main.
- Dirty unrelated files are not permission to reset, stash, or discard them.

## Verification
- Distinguish compile success from runtime success.
- Distinguish static inspection from runtime testing.
- Use PASS / FAIL / NOT TESTED honestly.
- Verify acceptance criteria individually.
- Report scope creep instead of silently implementing it.
- Do not claim a feature is DONE; final DONE approval belongs to Abim.

## Workflow
1. Read project instructions.
2. Inspect branch and git status.
3. Inspect relevant existing implementation.
4. Identify scope and acceptance criteria.
5. Reuse existing systems.
6. Implement the minimum required change.
7. Run available static/compile verification.
8. Report changed files, evidence, limitations, and assumptions.
9. Commit/push only when explicitly instructed.
10. Human performs required Unity Editor/runtime verification.
11. Review findings are resolved before final approval.

## Coding Style
- Keep architecture simple and scoped.
- Avoid unrelated refactors.
- Do not add comments unless they are necessary for a non-obvious constraint or explicitly requested.
- Keep production code organized by domain, not by developer name.

## Local Context
- PROJECT.md is Abim's historical/local context file and is not the shared source of truth.
- Do not commit PROJECT.md.
- When PROJECT.md conflicts with current repository state or these instructions, inspect the repository and follow current verified state.
