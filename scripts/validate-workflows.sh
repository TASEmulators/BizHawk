#!/bin/bash
# Workflow Validation and Testing Script
# Validates all GitHub Actions workflows for correctness and completeness

set -e

echo "=============================================="
echo "   WORKFLOW VALIDATION TEST SUITE"
echo "=============================================="
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/.."
WORKFLOW_DIR="$REPO_ROOT/.github/workflows"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0
WARNINGS=0

# Test 1: Check YAML syntax for all workflows
echo "Test 1: Validating YAML syntax..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if python3 -c "import yaml; yaml.safe_load(open('$workflow'))" 2>/dev/null; then
        echo -e "${GREEN}✓${NC} $filename - Valid YAML"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗${NC} $filename - Invalid YAML"
        FAILED=$((FAILED + 1))
    fi
done
echo ""

# Test 2: Check for timeout protection
echo "Test 2: Checking timeout protection..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if grep -q "timeout-minutes:" "$workflow"; then
        echo -e "${GREEN}✓${NC} $filename - Has timeout protection"
        PASSED=$((PASSED + 1))
    else
        echo -e "${YELLOW}⚠${NC} $filename - No timeout found (may use defaults)"
        WARNINGS=$((WARNINGS + 1))
    fi
done
echo ""

# Test 3: Check for proper branch references
echo "Test 3: Checking branch references..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if grep -qE "branches:.*\bmain\b|branches:.*\bdevelop\b" "$workflow" 2>/dev/null; then
        echo -e "${RED}✗${NC} $filename - Still uses main/develop branches"
        FAILED=$((FAILED + 1))
    else
        echo -e "${GREEN}✓${NC} $filename - Correct branch references"
        PASSED=$((PASSED + 1))
    fi
done
echo ""

# Test 4: Check for error handling patterns
echo "Test 4: Checking error handling..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if grep -q "continue-on-error\|EXIT_CODE\|set -e\|set +e" "$workflow"; then
        echo -e "${GREEN}✓${NC} $filename - Has error handling"
        PASSED=$((PASSED + 1))
    else
        echo -e "${YELLOW}⚠${NC} $filename - Basic error handling"
        WARNINGS=$((WARNINGS + 1))
    fi
done
echo ""

# Test 5: Check for GitHub Actions annotations
echo "Test 5: Checking for structured logging..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if grep -q "::notice::\|::warning::\|::error::" "$workflow"; then
        echo -e "${GREEN}✓${NC} $filename - Uses GitHub Actions annotations"
        PASSED=$((PASSED + 1))
    else
        echo -e "${YELLOW}⚠${NC} $filename - No annotations found"
        WARNINGS=$((WARNINGS + 1))
    fi
done
echo ""

# Test 6: Check workflow count
echo "Test 6: Checking workflow count..."
WORKFLOW_COUNT=$(find "$WORKFLOW_DIR" -name "*.yml" | wc -l)
if [ "$WORKFLOW_COUNT" -eq 8 ]; then
    echo -e "${GREEN}✓${NC} Found expected 8 workflows"
    PASSED=$((PASSED + 1))
else
    echo -e "${YELLOW}⚠${NC} Found $WORKFLOW_COUNT workflows (expected 8)"
    WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Test 7: Check for concurrency control
echo "Test 7: Checking concurrency control..."
for workflow in "$WORKFLOW_DIR"/*.yml; do
    filename=$(basename "$workflow")
    if grep -q "concurrency:" "$workflow"; then
        echo -e "${GREEN}✓${NC} $filename - Has concurrency control"
        PASSED=$((PASSED + 1))
    else
        echo -e "${YELLOW}⚠${NC} $filename - No concurrency control"
        WARNINGS=$((WARNINGS + 1))
    fi
done
echo ""

# Test 8: Check documentation exists
echo "Test 8: Checking documentation..."
if [ -f "$REPO_ROOT/WORKFLOW_OPTIMIZATION_SUMMARY.md" ]; then
    echo -e "${GREEN}✓${NC} WORKFLOW_OPTIMIZATION_SUMMARY.md exists"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗${NC} WORKFLOW_OPTIMIZATION_SUMMARY.md missing"
    FAILED=$((FAILED + 1))
fi

if [ -f "$REPO_ROOT/PR_AND_WORKFLOW_ANALYSIS.md" ]; then
    echo -e "${GREEN}✓${NC} PR_AND_WORKFLOW_ANALYSIS.md exists"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}✗${NC} PR_AND_WORKFLOW_ANALYSIS.md missing"
    FAILED=$((FAILED + 1))
fi
echo ""

# Test 9: Specific workflow checks
echo "Test 9: Specific workflow validation..."

# Check apk-build.yml has all jobs
if [ -f "$WORKFLOW_DIR/apk-build.yml" ]; then
    JOBS=$(grep -c "^  [a-z-]*:$" "$WORKFLOW_DIR/apk-build.yml" || echo "0")
    if [ "$JOBS" -ge 7 ]; then
        echo -e "${GREEN}✓${NC} apk-build.yml has all expected jobs ($JOBS)"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗${NC} apk-build.yml missing jobs (found $JOBS, expected 7+)"
        FAILED=$((FAILED + 1))
    fi
fi

# Check ci.yml has all jobs
if [ -f "$WORKFLOW_DIR/ci.yml" ]; then
    if grep -q "analyzer-build:" "$WORKFLOW_DIR/ci.yml" && \
       grep -q "test:" "$WORKFLOW_DIR/ci.yml" && \
       grep -q "package:" "$WORKFLOW_DIR/ci.yml"; then
        echo -e "${GREEN}✓${NC} ci.yml has all expected jobs"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗${NC} ci.yml missing expected jobs"
        FAILED=$((FAILED + 1))
    fi
fi
echo ""

# Summary
echo "=============================================="
echo "                SUMMARY"
echo "=============================================="
echo -e "${GREEN}Passed:${NC}   $PASSED"
echo -e "${YELLOW}Warnings:${NC} $WARNINGS"
echo -e "${RED}Failed:${NC}   $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All critical tests passed!${NC}"
    exit 0
else
    echo -e "${RED}✗ Some tests failed. Please review.${NC}"
    exit 1
fi
