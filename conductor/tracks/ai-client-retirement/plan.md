---
type: plan
title: Retire ardanova-ai-client — plan
---
# Plan — Retire ardanova-ai-client

- [x] Interim: service-key guard on every agent route, fail closed (loop 1).
- [ ] Verify no deployment or caller (Railway services, `git grep`).
- [ ] Owner go-ahead to delete.
- [ ] Remove the directory, Docker/CI/Railway entries and doc references in one commit.
