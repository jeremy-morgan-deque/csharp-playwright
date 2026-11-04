#!/bin/bash

# Generate HTML and CSV reports from the JSON results
axe reporter ./a11y-json ./a11y-reports --format=html,csv

# CSV with only violations and items needing review
#axe reporter ./a11y-json ./a11y-reports --format=csv --filter violations,incomplete
