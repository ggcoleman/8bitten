# 8Bitten Branch Management Script
# Automates common branching operations following the project's branching strategy

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("create-feature", "create-component", "create-research", "merge-feature", "create-release", "help")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$Name,
    
    [Parameter(Mandatory=$false)]
    [string]$Parent,
    
    [Parameter(Mandatory=$false)]
    [string]$Description,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

# Feature branch mapping from tasks.md
$FeatureBranches = @{
    "001" = @{ Name = "headless-execution"; Description = "US1: Headless ROM Execution (Foundation)" }
    "002" = @{ Name = "cli-gaming"; Description = "US2: Command Line Gaming" }
    "003" = @{ Name = "gui-configuration"; Description = "US3: GUI Configuration" }
    "004" = @{ Name = "ai-ml-platform"; Description = "US4: AI Training & ML Platform" }
    "005" = @{ Name = "documentation"; Description = "US5: Technical Documentation" }
    "006" = @{ Name = "research-analytics"; Description = "US6: Academic Research & Analytics" }
    "007" = @{ Name = "speedrun-analysis"; Description = "US7: Speedrunning Analysis" }
    "008" = @{ Name = "hardware-validation"; Description = "US8: Hardware Accuracy Validation" }
    "009" = @{ Name = "polish-optimization"; Description = "Final Polish & Optimization" }
}

# Component templates for each feature
$ComponentTemplates = @{
    "001-headless-execution" = @("cpu-implementation", "ppu-headless", "apu-silent", "memory-management", "timing-coordination", "diagnostic-output")
    "002-cli-gaming" = @("graphics-renderer", "audio-output", "input-handling", "game-window", "performance-monitoring")
    "003-gui-configuration" = @("main-window", "settings-panels", "configuration-persistence", "rom-browser")
    "004-ai-ml-platform" = @("mcp-server", "authentication", "game-state-api", "session-management")
    "005-documentation" = @("architecture-docs", "api-docs", "component-docs", "user-guides")
    "006-research-analytics" = @("metrics-collection", "data-export", "statistical-analysis", "session-recording")
    "007-speedrun-analysis" = @("overlay-system", "input-optimization", "run-comparison", "performance-profiling")
    "008-hardware-validation" = @("test-rom-integration", "accuracy-validation", "mapper-support", "edge-case-testing")
}

function Show-Help {
    Write-Host "8Bitten Branch Management Script" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\branch-management.ps1 -Action <action> [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Actions:" -ForegroundColor Yellow
    Write-Host "  create-feature    Create a new feature branch (001-009)" -ForegroundColor White
    Write-Host "  create-component  Create a component branch within a feature" -ForegroundColor White
    Write-Host "  create-research   Create a research/experimental branch" -ForegroundColor White
    Write-Host "  merge-feature     Merge completed feature to develop" -ForegroundColor White
    Write-Host "  create-release    Create a release branch" -ForegroundColor White
    Write-Host "  help             Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  # Create feature branch for US1" -ForegroundColor Green
    Write-Host "  .\branch-management.ps1 -Action create-feature -Name 001" -ForegroundColor White
    Write-Host ""
    Write-Host "  # Create CPU component branch within US1" -ForegroundColor Green
    Write-Host "  .\branch-management.ps1 -Action create-component -Parent 001-headless-execution -Name cpu-implementation" -ForegroundColor White
    Write-Host ""
    Write-Host "  # Create research branch" -ForegroundColor Green
    Write-Host "  .\branch-management.ps1 -Action create-research -Name determinism-analysis -Description 'Cross-platform replay validation'" -ForegroundColor White
    Write-Host ""
    Write-Host "Available Features:" -ForegroundColor Yellow
    foreach ($key in $FeatureBranches.Keys | Sort-Object) {
        $feature = $FeatureBranches[$key]
        Write-Host "  $key - $($feature.Name): $($feature.Description)" -ForegroundColor White
    }
}

function Test-GitRepository {
    if (-not (Test-Path ".git")) {
        Write-Error "Not in a Git repository. Please run from the repository root."
        exit 1
    }
}

function Get-CurrentBranch {
    return (git branch --show-current)
}

function Test-BranchExists {
    param([string]$BranchName)
    $branches = git branch -a | ForEach-Object { $_.Trim().Replace("* ", "").Replace("remotes/origin/", "") }
    return $branches -contains $BranchName
}

function Create-FeatureBranch {
    param([string]$FeatureNumber)
    
    if (-not $FeatureBranches.ContainsKey($FeatureNumber)) {
        Write-Error "Invalid feature number. Use 001-009. Run with -Action help to see available features."
        exit 1
    }
    
    $feature = $FeatureBranches[$FeatureNumber]
    $branchName = "feature/$FeatureNumber-$($feature.Name)"
    
    Write-Host "Creating feature branch: $branchName" -ForegroundColor Cyan
    Write-Host "Description: $($feature.Description)" -ForegroundColor Gray
    
    if (Test-BranchExists $branchName) {
        Write-Error "Branch $branchName already exists."
        exit 1
    }
    
    if ($DryRun) {
        Write-Host "[DRY RUN] Would execute:" -ForegroundColor Yellow
        Write-Host "  git checkout develop" -ForegroundColor Gray
        Write-Host "  git pull origin develop" -ForegroundColor Gray
        Write-Host "  git checkout -b $branchName" -ForegroundColor Gray
        return
    }
    
    # Ensure we're on develop and up to date
    git checkout develop
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to checkout develop"; exit 1 }
    
    git pull origin develop
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to pull develop"; exit 1 }
    
    # Create feature branch
    git checkout -b $branchName
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create branch"; exit 1 }
    
    Write-Host "✅ Created feature branch: $branchName" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Create component branches for parallel development:" -ForegroundColor White
    
    if ($ComponentTemplates.ContainsKey("$FeatureNumber-$($feature.Name)")) {
        foreach ($component in $ComponentTemplates["$FeatureNumber-$($feature.Name)"]) {
            Write-Host "   .\branch-management.ps1 -Action create-component -Parent $branchName -Name $component" -ForegroundColor Gray
        }
    }
    
    Write-Host "2. Start implementing tasks from specs/001-cycle-accurate-emulator/tasks.md" -ForegroundColor White
    Write-Host "3. Commit regularly with clear messages (see docs/development/commit-message-guide.md)" -ForegroundColor White
}

function Create-ComponentBranch {
    param([string]$ParentBranch, [string]$ComponentName)
    
    if (-not $ParentBranch -or -not $ComponentName) {
        Write-Error "Both -Parent and -Name are required for component branches."
        exit 1
    }
    
    $branchName = "$ParentBranch/$ComponentName"
    
    Write-Host "Creating component branch: $branchName" -ForegroundColor Cyan
    
    if (Test-BranchExists $branchName) {
        Write-Error "Branch $branchName already exists."
        exit 1
    }
    
    if (-not (Test-BranchExists $ParentBranch)) {
        Write-Error "Parent branch $ParentBranch does not exist. Create it first."
        exit 1
    }
    
    if ($DryRun) {
        Write-Host "[DRY RUN] Would execute:" -ForegroundColor Yellow
        Write-Host "  git checkout $ParentBranch" -ForegroundColor Gray
        Write-Host "  git pull origin $ParentBranch" -ForegroundColor Gray
        Write-Host "  git checkout -b $branchName" -ForegroundColor Gray
        return
    }
    
    # Checkout parent branch and create component branch
    git checkout $ParentBranch
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to checkout $ParentBranch"; exit 1 }
    
    git checkout -b $branchName
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create branch"; exit 1 }
    
    Write-Host "✅ Created component branch: $branchName" -ForegroundColor Green
    Write-Host ""
    Write-Host "Development tips:" -ForegroundColor Yellow
    Write-Host "- Focus on single component implementation" -ForegroundColor White
    Write-Host "- Write tests first (TDD approach)" -ForegroundColor White
    Write-Host "- Commit frequently with clear scope" -ForegroundColor White
    Write-Host "- Merge back to parent when component is complete" -ForegroundColor White
}

function Create-ResearchBranch {
    param([string]$ResearchName, [string]$ResearchDescription)
    
    if (-not $ResearchName) {
        Write-Error "-Name is required for research branches."
        exit 1
    }
    
    $branchName = "research/$ResearchName"
    
    Write-Host "Creating research branch: $branchName" -ForegroundColor Cyan
    if ($ResearchDescription) {
        Write-Host "Description: $ResearchDescription" -ForegroundColor Gray
    }
    
    if (Test-BranchExists $branchName) {
        Write-Error "Branch $branchName already exists."
        exit 1
    }
    
    if ($DryRun) {
        Write-Host "[DRY RUN] Would execute:" -ForegroundColor Yellow
        Write-Host "  git checkout develop" -ForegroundColor Gray
        Write-Host "  git pull origin develop" -ForegroundColor Gray
        Write-Host "  git checkout -b $branchName" -ForegroundColor Gray
        return
    }
    
    # Create research branch from develop
    git checkout develop
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to checkout develop"; exit 1 }
    
    git pull origin develop
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to pull develop"; exit 1 }
    
    git checkout -b $branchName
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create branch"; exit 1 }
    
    Write-Host "✅ Created research branch: $branchName" -ForegroundColor Green
    Write-Host ""
    Write-Host "Research guidelines:" -ForegroundColor Yellow
    Write-Host "- Use 'research(scope):' commit prefix" -ForegroundColor White
    Write-Host "- Include experiment ID and research question" -ForegroundColor White
    Write-Host "- Document methodology and validation approach" -ForegroundColor White
    Write-Host "- Share branch for peer review and collaboration" -ForegroundColor White
}

# Main execution
Test-GitRepository

switch ($Action) {
    "help" { Show-Help }
    "create-feature" { Create-FeatureBranch -FeatureNumber $Name }
    "create-component" { Create-ComponentBranch -ParentBranch $Parent -ComponentName $Name }
    "create-research" { Create-ResearchBranch -ResearchName $Name -ResearchDescription $Description }
    default { Write-Error "Unknown action: $Action. Use -Action help for usage information." }
}
