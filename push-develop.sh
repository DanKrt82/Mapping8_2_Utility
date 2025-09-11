#!/bin/bash

# Script to push the develop branch to the remote repository
# This script should be run after proper authentication is configured

echo "Pushing develop branch to remote repository..."

# Switch to develop branch
git checkout develop

# Push develop branch to origin with upstream tracking
git push -u origin develop

echo "Develop branch has been pushed to origin successfully!"
echo "The develop branch is now available for team collaboration."