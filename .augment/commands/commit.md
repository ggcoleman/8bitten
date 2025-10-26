Command Description: Execute the 8Bitten commit process following project standards and branching strategy.

Prompt to process:
## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Pre-commit validation**: Check current branch, working directory status, and constitutional compliance
2. **Analyze changes**: Review staged/unstaged changes and determine appropriate commit scope and type
3. **Generate commit message**: Create properly formatted commit message following 8Bitten standards
4. **Execute commit**: Stage changes and commit with validated message
5. **Post-commit actions**: Suggest next steps based on branch type and development workflow

## 8Bitten Commit Standards

### **Commit Message Format**
```
<type>(<scope>): <description>

[optional body explaining what and why]

[optional footer with issue references]
```

### **Commit Types**
| Type | Purpose | Example |
|------|---------|---------|
| `feat` | New feature implementation | `feat(cpu): implement 6502 instruction set` |
| `fix` | Bug fixes | `fix(ppu): correct scanline timing calculation` |
| `docs` | Documentation changes | `docs(api): add MCP interface examples` |
| `test` | Test additions or modifications | `test(cpu): add comprehensive instruction tests` |
| `refactor` | Code refactoring without feature changes | `refactor(memory): optimize bank switching logic` |
| `perf` | Performance improvements | `perf(timing): reduce cycle calculation overhead` |
| `research` | Research and experimental features | `research(metrics): implement frame timing analysis` |
| `validate` | Accuracy validation and testing | `validate(cpu): verify against Blargg test suite` |

### **Scope Guidelines**
| Scope | Components | Example |
|-------|------------|---------|
| `cpu` | 6502 processor emulation | `feat(cpu): add interrupt handling` |
| `ppu` | Picture Processing Unit | `fix(ppu): correct sprite evaluation timing` |
| `apu` | Audio Processing Unit | `feat(apu): implement triangle wave channel` |
| `memory` | Memory management | `refactor(memory): optimize mapper switching` |
| `cartridge` | ROM loading and mappers | `feat(cartridge): add MMC3 mapper support` |
| `timing` | Synchronization and coordination | `perf(timing): improve cycle accuracy` |
| `cli` | Command-line interface | `feat(cli): add diagnostic output options` |
| `gui` | Graphical user interface | `feat(gui): implement settings persistence` |
| `mcp` | AI agent communication | `feat(mcp): add session management` |
| `metrics` | Performance and research metrics | `feat(metrics): add input latency measurement` |
| `recording` | Input recording and replay | `feat(recording): implement deterministic replay` |
| `analysis` | Statistical analysis tools | `feat(analysis): add run comparison algorithms` |
| `docs` | Documentation | `docs(architecture): add component diagrams` |
| `test` | Testing infrastructure | `test(integration): add ROM compatibility tests` |

### **Branch Context Awareness**
Adjust commit approach based on current branch:

#### **Feature Branches** (`feature/###-name`)
- Focus on user story completion
- Reference task IDs from tasks.md (e.g., "Ref: T020")
- Include constitutional compliance notes
- Emphasize research-grade quality

#### **Component Branches** (`feature/###-name/component`)
- Focus on single component implementation
- Detailed technical implementation notes
- Performance and accuracy metrics
- Integration considerations

#### **Research Branches** (`research/name`)
- Include experiment ID and research question
- Document methodology and validation approach
- Reference academic standards and peer review
- Include statistical significance and confidence intervals

#### **Documentation Branches** (`docs/section`)
- Focus on clarity and completeness
- Include examples and usage scenarios
- Reference related code changes
- Ensure academic and professional standards

### **Constitutional Compliance Check**
Before committing, verify adherence to 8Bitten's 7 constitutional principles:

1. **Test-Driven Development**: Are tests included/updated?
2. **Component-Based Architecture**: Does change maintain separation of concerns?
3. **Performance-First Design**: Does change meet performance targets?
4. **NET Standard Compliance**: Follows .NET 9.0 and C# 13.0 standards?
5. **Comprehensive Integration Testing**: Integration impacts considered?
6. **Research-Grade Metrics**: Research features meet academic standards?
7. **Universal Accessibility**: Change serves all user communities?

### **Quality Checklist**
- [ ] Code compiles without errors
- [ ] Basic tests pass
- [ ] Documentation updated (if applicable)
- [ ] Performance impact assessed
- [ ] Research accuracy maintained (if applicable)
- [ ] Constitutional principles followed
- [ ] Commit message follows format standards

## Execution Steps

1. **Analyze Current State**:
   ```bash
   # Check current branch and status
   git branch --show-current
   git status --porcelain
   git diff --staged --name-only
   git diff --name-only
   ```

2. **Determine Commit Scope**:
   - Identify primary component(s) affected
   - Determine if change is feat, fix, docs, test, etc.
   - Assess impact on research/accuracy requirements
   - Check for multi-component changes

3. **Generate Commit Message**:
   - Use appropriate type and scope
   - Write clear, concise description (<50 chars)
   - Add detailed body if needed (what and why)
   - Include task references and issue links
   - Add constitutional compliance notes

4. **Execute Commit**:
   ```bash
   # Stage all changes (or selective staging)
   git add .
   # OR selective staging
   git add <specific-files>
   
   # Commit with generated message
   git commit -m "<generated-message>"
   ```

5. **Post-Commit Actions**:
   - Suggest next development steps
   - Recommend testing or validation
   - Identify integration opportunities
   - Note documentation needs

## Example Workflows

### **Feature Implementation Commit**
```bash
# Current: feature/001-headless-execution/cpu-implementation
# Changes: src/Core/CPU/Instructions/, tests/Unit/CPU/

git add src/Core/CPU/Instructions/ tests/Unit/CPU/
git commit -m "feat(cpu): implement 6502 arithmetic instructions (ADC, SBC)

Add complete implementation of arithmetic instructions with:
- Carry flag handling and overflow detection
- Decimal mode support for ADC/SBC
- Cycle-accurate timing for all addressing modes
- Comprehensive test coverage with edge cases

Maintains constitutional compliance:
- TDD: Tests written first and passing
- Performance: Optimized instruction decode
- Accuracy: Validated against NESdev Wiki specs

Ref: T020, T021"
```

### **Research Feature Commit**
```bash
# Current: research/determinism-analysis
# Changes: src/Infrastructure/Analysis/, docs/research/

git add src/Infrastructure/Analysis/ docs/research/
git commit -m "research(analysis): implement cross-platform determinism validation

Experimental framework for validating deterministic replay:
- State hash comparison with SHA-256
- Timing variance analysis with statistical significance
- Cross-platform validation (Windows, macOS, Linux)
- Confidence interval calculation (95% CI)

Research Question: Can replay accuracy be maintained across platforms?
Methodology: Compare state hashes every 1000 cycles
Validation: Statistical analysis of timing variance
Expected Outcome: <0.1% variance in replay accuracy

Experiment ID: EXP-2025-001
For peer review and academic collaboration."
```

### **Bug Fix Commit**
```bash
# Current: feature/002-cli-gaming
# Changes: src/Core/PPU/Renderer.cs

git add src/Core/PPU/Renderer.cs
git commit -m "fix(ppu): correct sprite overflow flag timing for research accuracy

Sprite overflow flag was set one cycle early, affecting deterministic
replay accuracy for ML training applications.

Root cause: Incorrect cycle counting in sprite evaluation
Fix: Align timing with NESdev Wiki sprite evaluation documentation
Impact: Ensures 100% deterministic replay for research

Validated against: Blargg sprite_overflow_timing test ROM
Research impact: Critical for ML training data consistency

Fixes #45"
```

### **Documentation Commit**
```bash
# Current: docs/api
# Changes: docs/API/MCP/, docs/research/

git add docs/API/MCP/ docs/research/
git commit -m "docs(mcp): add comprehensive AI agent integration guide

Complete documentation for AI/ML researchers including:
- MCP protocol implementation with authentication examples
- Game state data structures and extraction methods
- Session management and error handling patterns
- Performance considerations for real-time training

Includes working code examples for:
- Python AI agent connection
- TensorFlow integration patterns
- Real-time state observation
- Automated gameplay sessions

Enables academic collaboration and research adoption.
Supports constitutional principle of universal accessibility."
```

## Key Rules

1. **Always check branch context** - commit style varies by branch type
2. **Reference constitutional principles** - especially for feature commits
3. **Include research impact** - for accuracy or analysis changes
4. **Use task references** - link to tasks.md items when applicable
5. **Maintain quality standards** - every commit should be production-ready
6. **Consider integration** - note impacts on other components
7. **Document methodology** - especially for research and experimental work

## Success Criteria

- **Clear Intent**: Purpose is immediately obvious from commit message
- **Appropriate Scope**: Changes match declared scope and type
- **Complete Unit**: Represents finished, testable work
- **Constitutional Compliance**: Adheres to all 7 project principles
- **Research Standards**: Meets academic rigor for research features
- **Community Value**: Contributes to project goals and user communities

This command ensures every commit maintains 8Bitten's research-grade standards while supporting collaborative development across academic, competitive gaming, and open-source communities.
