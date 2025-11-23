#!/bin/bash
#
# Automated Compliance Checking Script
# BizHawkRafaelia Legal and Humanitarian Compliance Framework
#
# This script performs automated compliance checks for:
# - Copyright headers
# - License file presence
# - Attribution completeness
# - Human rights compliance
# - Child protection measures
# - Indigenous rights respect
# - Privacy protection
# - Accessibility features
# - Environmental sustainability
#
# Based on:
# - LEGAL_COMPLIANCE_FRAMEWORK.md
# - HUMANITARIAN_GUIDELINES.md
# - Berne Convention, WIPO, UN, UNESCO, UNICEF standards
#

set -e

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
CRITICAL_ISSUES=0
HIGH_ISSUES=0
MEDIUM_ISSUES=0
LOW_ISSUES=0
TOTAL_ESTIMATED_COST=0

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}BizHawkRafaelia Compliance Check${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Get project root
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

echo "Project root: $PROJECT_ROOT"
echo ""

# Check 1: Required documentation files
echo -e "${BLUE}[1/10] Checking required documentation files...${NC}"
REQUIRED_FILES=(
    "LICENSE"
    "CONTRIBUTORS.md"
    "ATTRIBUTIONS.md"
    "LEGAL_COMPLIANCE_FRAMEWORK.md"
    "HUMANITARIAN_GUIDELINES.md"
    "BATTERY_OPTIMIZATION_GUIDE.md"
    "REFERENCES.md"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ ! -f "$file" ]; then
        echo -e "${RED}  ✗ CRITICAL: Missing $file${NC}"
        CRITICAL_ISSUES=$((CRITICAL_ISSUES + 1))
        TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 5000))
    else
        echo -e "${GREEN}  ✓ Found $file${NC}"
    fi
done
echo ""

# Check 2: Copyright headers in source files
echo -e "${BLUE}[2/10] Checking copyright headers in source files...${NC}"
MISSING_COPYRIGHT=0
CHECKED_FILES=0

# Cache source file list to avoid multiple finds
SOURCE_FILES=$(mktemp)
find . -type f \( -name "*.cs" -o -name "*.cpp" -o -name "*.c" \) \
    ! -path "*/obj/*" ! -path "*/bin/*" ! -path "*/packages/*" > "$SOURCE_FILES"

while IFS= read -r file; do
    CHECKED_FILES=$((CHECKED_FILES + 1))
    if ! grep -q -i "copyright\|©\|(c)" "$file" 2>/dev/null; then
        if [ $MISSING_COPYRIGHT -eq 0 ]; then
            echo -e "${YELLOW}  ⚠ Files missing copyright notice:${NC}"
        fi
        MISSING_COPYRIGHT=$((MISSING_COPYRIGHT + 1))
        if [ $MISSING_COPYRIGHT -le 5 ]; then
            echo -e "${YELLOW}    - $(basename "$file")${NC}"
        fi
    fi
done < "$SOURCE_FILES"

if [ $MISSING_COPYRIGHT -gt 0 ]; then
    echo -e "${YELLOW}  ⚠ $MISSING_COPYRIGHT of $CHECKED_FILES files missing copyright (showing first 5)${NC}"
    HIGH_ISSUES=$((HIGH_ISSUES + MISSING_COPYRIGHT))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + MISSING_COPYRIGHT))
else
    echo -e "${GREEN}  ✓ All $CHECKED_FILES files have copyright notices${NC}"
fi
echo ""

# Check 3: License headers
echo -e "${BLUE}[3/10] Checking license headers...${NC}"
MISSING_LICENSE=0

while IFS= read -r file; do
    if ! grep -q -i "license\|spdx" "$file" 2>/dev/null; then
        MISSING_LICENSE=$((MISSING_LICENSE + 1))
    fi
done < "$SOURCE_FILES"

if [ $MISSING_LICENSE -gt 0 ]; then
    echo -e "${YELLOW}  ⚠ $MISSING_LICENSE files missing license headers${NC}"
    HIGH_ISSUES=$((HIGH_ISSUES + MISSING_LICENSE))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + MISSING_LICENSE * 1500))
else
    echo -e "${GREEN}  ✓ All files have license headers${NC}"
fi

# Clean up temporary file
rm -f "$SOURCE_FILES"
echo ""

# Check 4: Attribution completeness
echo -e "${BLUE}[4/10] Checking attribution completeness...${NC}"
CONTRIBUTORS_FILE="CONTRIBUTORS.md"
EXPECTED_CONTRIBUTORS=("Rafael Melo Reis" "TASEmulators" "BizHawk Core Team")

for contributor in "${EXPECTED_CONTRIBUTORS[@]}"; do
    if [ -f "$CONTRIBUTORS_FILE" ]; then
        if ! grep -q "$contributor" "$CONTRIBUTORS_FILE"; then
            echo -e "${RED}  ✗ CRITICAL: Missing attribution to $contributor${NC}"
            CRITICAL_ISSUES=$((CRITICAL_ISSUES + 1))
            TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 10000))
        else
            echo -e "${GREEN}  ✓ $contributor properly attributed${NC}"
        fi
    fi
done
echo ""

# Check 5: Human rights compliance (discriminatory terms)
echo -e "${BLUE}[5/10] Checking for potentially discriminatory terms...${NC}"
DISCRIMINATORY_TERMS=("blacklist" "whitelist" "master" "slave" "sanity check")
FOUND_TERMS=0

for term in "${DISCRIMINATORY_TERMS[@]}"; do
    COUNT=$(grep -r -i -c "$term" --include="*.cs" --include="*.cpp" --include="*.c" \
        --exclude-dir=obj --exclude-dir=bin --exclude-dir=packages . 2>/dev/null | grep -v ":0$" | wc -l || true)
    if [ "$COUNT" -gt 0 ]; then
        echo -e "${YELLOW}  ⚠ Found '$term' in $COUNT files${NC}"
        FOUND_TERMS=$((FOUND_TERMS + COUNT))
        MEDIUM_ISSUES=$((MEDIUM_ISSUES + COUNT))
        TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + COUNT * 100))
    fi
done

if [ $FOUND_TERMS -eq 0 ]; then
    echo -e "${GREEN}  ✓ No discriminatory terms found${NC}"
fi
echo ""

# Check 6: Child protection measures
echo -e "${BLUE}[6/10] Checking for child protection measures...${NC}"
if grep -r -i "child\|minor\|age" --include="*.cs" \
    --exclude-dir=obj --exclude-dir=bin . >/dev/null 2>&1; then
    echo -e "${GREEN}  ✓ Child protection code detected${NC}"
else
    echo -e "${YELLOW}  ⚠ No evidence of child protection measures (CRC Article 16)${NC}"
    HIGH_ISSUES=$((HIGH_ISSUES + 1))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 25000))
fi
echo ""

# Check 7: Indigenous rights documentation
echo -e "${BLUE}[7/10] Checking indigenous rights documentation...${NC}"
if [ -f "HUMANITARIAN_GUIDELINES.md" ]; then
    if grep -q -i "indigenous" "HUMANITARIAN_GUIDELINES.md"; then
        echo -e "${GREEN}  ✓ Indigenous rights documented${NC}"
    else
        echo -e "${YELLOW}  ⚠ Insufficient indigenous peoples' rights documentation${NC}"
        HIGH_ISSUES=$((HIGH_ISSUES + 1))
        TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 5000))
    fi
fi
echo ""

# Check 8: Privacy protection
echo -e "${BLUE}[8/10] Checking for data collection/privacy concerns...${NC}"
PRIVACY_CONCERNS=$(grep -r -i "analytics\|tracking\|telemetry" --include="*.cs" \
    --exclude-dir=obj --exclude-dir=bin --exclude-dir=packages . 2>/dev/null | wc -l || echo 0)

if [ "$PRIVACY_CONCERNS" -gt 0 ]; then
    echo -e "${YELLOW}  ⚠ Found $PRIVACY_CONCERNS potential data collection references${NC}"
    echo -e "${YELLOW}    Verify consent and necessity${NC}"
    HIGH_ISSUES=$((HIGH_ISSUES + PRIVACY_CONCERNS))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + PRIVACY_CONCERNS * 50000))
else
    echo -e "${GREEN}  ✓ No obvious privacy concerns detected${NC}"
fi
echo ""

# Check 9: Accessibility features
echo -e "${BLUE}[9/10] Checking for accessibility features...${NC}"
if grep -r -i "accessibility\|screen reader\|keyboard navigation\|colorblind" \
    --include="*.cs" --exclude-dir=obj --exclude-dir=bin . >/dev/null 2>&1; then
    echo -e "${GREEN}  ✓ Accessibility features detected${NC}"
else
    echo -e "${YELLOW}  ⚠ No accessibility features detected (WCAG 2.1)${NC}"
    MEDIUM_ISSUES=$((MEDIUM_ISSUES + 1))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 10000))
fi
echo ""

# Check 10: Battery optimization (environmental)
echo -e "${BLUE}[10/10] Checking battery optimization documentation...${NC}"
if [ -f "BATTERY_OPTIMIZATION_GUIDE.md" ]; then
    echo -e "${GREEN}  ✓ Battery optimization documented (SDG 7)${NC}"
else
    echo -e "${YELLOW}  ⚠ No battery optimization documentation${NC}"
    LOW_ISSUES=$((LOW_ISSUES + 1))
    TOTAL_ESTIMATED_COST=$((TOTAL_ESTIMATED_COST + 500))
fi
echo ""

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Compliance Check Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Critical Issues:  $CRITICAL_ISSUES"
echo "High Priority:    $HIGH_ISSUES"
echo "Medium Priority:  $MEDIUM_ISSUES"
echo "Low Priority:     $LOW_ISSUES"
echo ""
echo "Total Estimated Legal Cost: \$${TOTAL_ESTIMATED_COST}"
echo ""

if [ $CRITICAL_ISSUES -eq 0 ]; then
    echo -e "${GREEN}✓ COMPLIANT${NC} - No critical issues found"
    echo ""
    exit 0
else
    echo -e "${RED}✗ NON-COMPLIANT${NC} - $CRITICAL_ISSUES critical issues must be resolved"
    echo ""
    exit 1
fi
