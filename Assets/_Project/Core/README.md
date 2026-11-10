# Core — `DebtDungeon.Core`

**Owner:** Christopher (team lead). Everyone reads it; only Chris changes it.

Core is the contract every module codes against: shared enums, data structs, interfaces and the `GameEvents` hub.
Modules never reference each other, only Core, so Core is how they talk.

- **References:** nothing except Unity.
- **Every public type and member here gets an XML `/// <summary>` comment.**
- **Changing Core:** ask in Discord. Chris makes the change on `feature/chris-core`, bumps the version, logs it in
  `CHANGELOG.md`, lists which modules are affected, and announces it in Discord when it merges into `develop`.

Current version and pending changes: see `CHANGELOG.md` in this folder.
