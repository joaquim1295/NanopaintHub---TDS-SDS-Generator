---
name: Git Workflow Automation
description: Always commit changes when work is completed.
---

# Git Workflow Rules

You MUST automatically commit all functional changes using Git without waiting for the user to explicitly ask you to do so.

## When to Commit
- Every time you successfully implement a feature, fix a bug, or make structural code changes.
- After recovering or restoring code.
- At logical stopping points during a complex refactoring.

## How to Commit
1. Run `git status` to see what changed.
2. Run `git add <files>` for the specific files you worked on, or `git add .` if everything should be included.
3. Run `git commit -m "Your descriptive message here"` explaining what you built or fixed in Portuguese (PT-PT).

## Importance
This is a core requirement for this workspace to ensure that code is never accidentally lost if the user uses commands like `git restore` or changes branches.
