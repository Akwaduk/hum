#!/usr/bin/env bash
# Script to test hum's GitHub integration

echo "Testing hum GitHub integration..."

# Check prereqs
echo "Checking prerequisites..."
if ! command -v gh &> /dev/null; then
    echo "GitHub CLI (gh) not found! Please install it first."
    exit 1
fi

if ! gh auth status &> /dev/null; then
    echo "Not authenticated with GitHub. Please run 'gh auth login' first."
    exit 1
fi

# Generate a unique test repo name with timestamp
TIMESTAMP=$(date +%Y%m%d%H%M%S)
TEST_REPO_NAME="hum-test-$TIMESTAMP"

# Create a temporary project
echo "Creating test project..."
mkdir -p /tmp/$TEST_REPO_NAME
cd /tmp/$TEST_REPO_NAME

# Create a test file
echo "# Test Repository" > README.md
echo "Created for testing hum GitHub integration on $TIMESTAMP" >> README.md

# Initialize git
git init
git add README.md
git commit -m "Initial commit"

# Test hum create with GitHub integration
echo "Testing hum create with GitHub integration..."
cd ..
dotnet run --project /path/to/hum/project create $TEST_REPO_NAME --template dotnet-minimal --dir /tmp/$TEST_REPO_NAME

# Check results
echo "Testing if repository was created on GitHub..."
if gh repo view $TEST_REPO_NAME &> /dev/null; then
    echo "✅ Success! Repository was created on GitHub"
else
    echo "❌ Failed! Repository was not created on GitHub"
    exit 1
fi

# Cleanup
echo "Cleaning up test repository..."
gh repo delete $TEST_REPO_NAME --yes

echo "Test completed successfully!"
