# Commit Message Quick Reference Guide

**Purpose**: Quick reference for writing clear, consistent commit messages in 8Bitten development.

## 🚀 **Quick Format**

```
<type>(<scope>): <description>

[optional body explaining what and why]

[optional footer with issue references]
```

## 📋 **Types Reference**

| Type | When to Use | Example |
|------|-------------|---------|
| `feat` | New features or capabilities | `feat(cpu): implement 6502 instruction set` |
| `fix` | Bug fixes | `fix(ppu): correct scanline timing` |
| `docs` | Documentation only | `docs(api): add MCP examples` |
| `test` | Tests only | `test(cpu): add instruction validation` |
| `refactor` | Code restructuring | `refactor(memory): optimize bank switching` |
| `perf` | Performance improvements | `perf(timing): reduce cycle overhead` |
| `research` | Research/experimental | `research(metrics): frame analysis` |
| `validate` | Accuracy validation | `validate(cpu): verify Blargg tests` |

## 🎯 **Scopes Reference**

| Scope | Components |
|-------|------------|
| `cpu` | 6502 processor emulation |
| `ppu` | Picture Processing Unit |
| `apu` | Audio Processing Unit |
| `memory` | Memory management |
| `cartridge` | ROM loading, mappers |
| `timing` | Synchronization |
| `cli` | Command-line interface |
| `gui` | Graphical interface |
| `mcp` | AI agent communication |
| `metrics` | Performance metrics |
| `recording` | Input recording/replay |
| `analysis` | Statistical analysis |
| `docs` | Documentation |
| `test` | Testing infrastructure |

## ✅ **Good Examples**

```bash
# Feature with clear scope and impact
feat(cpu): implement 6502 addressing modes

Add complete addressing mode support:
- Immediate, Zero Page, Absolute
- Indexed with X/Y registers
- Indirect for JMP instruction

Includes cycle-accurate timing and comprehensive tests.
Closes #T020

# Bug fix with research impact
fix(ppu): correct sprite overflow timing for deterministic replay

Sprite overflow flag was set one cycle early, breaking replay
accuracy for ML training. Updated to match NESdev Wiki timing.

Validated: Blargg sprite_overflow_timing test ROM
Research impact: Ensures deterministic replay

Fixes #45

# Performance optimization
perf(timing): optimize cycle calculation (15% improvement)

- Lookup table for instruction cycles
- Eliminate redundant timing checks
- Optimize component synchronization

Maintains accuracy, improves performance to 60 FPS target.

# Research contribution
research(analysis): implement frame timing statistical analysis

Add comprehensive timing analysis with:
- Per-component cycle distribution
- Frame boundary detection
- Sub-frame precision measurement
- Confidence interval calculation

Supports academic research reproducibility.
Export formats: CSV, JSON, HDF5

# Documentation update
docs(mcp): add AI agent integration examples

Complete guide for AI/ML researchers:
- Authentication and session setup
- Game state data extraction
- Programmatic control examples
- Error handling patterns

Enables research collaboration and adoption.
```

## ❌ **Avoid These**

```bash
# Too vague
fix: bug fix
feat: add stuff
docs: update

# Missing scope
feat: implement CPU
fix: timing issue

# No description
feat(cpu):
fix(ppu): fix

# Implementation details in title
feat(cpu): add ADC instruction with carry flag handling and overflow detection

# Should be:
feat(cpu): implement arithmetic instructions (ADC, SBC)
```

## 🔄 **Workflow Integration**

### **Before Committing**

1. **Review changes**: `git diff --staged`
2. **Check scope**: What component(s) are affected?
3. **Identify type**: Feature, fix, docs, test, etc.
4. **Write description**: What does this change do?
5. **Add context**: Why was this change needed?
6. **Reference issues**: Link to tasks or bugs

### **Commit Frequency**

- **Daily commits** for active development
- **Logical units** of work (complete function, test suite, etc.)
- **Working state** - code should compile and basic tests pass
- **Incremental progress** - small, focused changes

### **Multi-Component Changes**

```bash
# If change affects multiple components, choose primary scope
feat(timing): synchronize CPU and PPU cycle coordination

Update both CPU and PPU components to maintain proper
synchronization during frame rendering.

Components affected:
- CPU: cycle counting and interrupt timing
- PPU: scanline synchronization
- Timing: coordination algorithms

# Or split into multiple commits
feat(cpu): update cycle counting for PPU synchronization
feat(ppu): implement scanline sync with CPU timing
feat(timing): add CPU-PPU coordination algorithms
```

## 📊 **Quality Metrics**

### **Good Commit Characteristics**

- **Clear intent**: Purpose is immediately obvious
- **Appropriate scope**: Changes match the declared scope
- **Complete unit**: Represents finished work
- **Testable**: Can be validated independently
- **Documented**: Includes necessary documentation updates

### **Commit Message Quality Checklist**

- [ ] Type accurately describes the change
- [ ] Scope matches affected components
- [ ] Description is clear and concise (<50 chars)
- [ ] Body explains what and why (if needed)
- [ ] References relevant issues/tasks
- [ ] Follows constitutional principles
- [ ] Maintains research-grade standards

## 🎯 **Special Cases**

### **Research Commits**

```bash
research(metrics): implement determinism validation framework

Experimental validation system for cross-platform determinism:
- State hash comparison algorithms
- Timing variance analysis
- Statistical significance testing

Experiment ID: EXP-2025-001
Research Question: Platform-independent replay accuracy
Validation: Compare against hardware reference

For peer review and academic collaboration.
```

### **Breaking Changes**

```bash
feat(api)!: redesign MCP interface for improved performance

BREAKING CHANGE: MCP authentication now requires JWT tokens
instead of simple API keys. Update AI agents accordingly.

Migration guide: docs/migration/mcp-v2.md
Performance improvement: 40% reduction in auth overhead
```

### **Hotfixes**

```bash
hotfix(cpu): fix critical timing bug in interrupt handling

Interrupt timing was off by one cycle, causing crashes in
specific games. Emergency fix for production release.

Affected games: Mega Man 2, Castlevania
Root cause: Incorrect cycle counting in IRQ handler
Validation: All affected games now work correctly

Fixes #CRITICAL-001
```

---

**Remember**: Good commit messages are documentation for future developers (including yourself). Write them as if explaining to a colleague what you changed and why.**
