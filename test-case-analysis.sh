#!/bin/bash

echo "Testing Case Analysis API Endpoints"
echo "=================================="

# Start the API in background
echo "Starting API..."
dotnet run --project BetterCallSaul.API &
API_PID=$!

# Wait for API to start
sleep 5

echo ""
echo "API Endpoints:"
echo "- POST /api/legalresearch/analyze/case/{caseId}/document/{documentId}"
echo "- GET /api/legalresearch/analysis/{analysisId}"
echo "- GET /api/legalresearch/case/{caseId}/analyses"
echo ""

# Test health endpoint first
echo "Testing health endpoint..."
curl -s http://localhost:5022/api/legalresearch/health | jq .

echo ""
echo "Case analysis implementation is complete!"
echo "To test with real Azure OpenAI, configure these settings in appsettings.json:"
echo ""
echo '"AzureOpenAI": {'
echo '  "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",'
echo '  "ApiKey": "YOUR_API_KEY",'
echo '  "DeploymentName": "gpt-4",'
echo '  "Model": "gpt-4"'
echo '}'
echo ""

# Stop the API
kill $API_PID 2>/dev/null
echo "API stopped"