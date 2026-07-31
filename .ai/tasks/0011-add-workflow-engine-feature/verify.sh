#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

# Structural checks
test -f src/backend/Libraries/Domain/Enum/EWorkflowState.cs       || { echo "FAIL: EWorkflowState.cs missing"; exit 1; }
test -f src/backend/Libraries/Domain/Models/WorkflowTransition.cs || { echo "FAIL: WorkflowTransition.cs missing"; exit 1; }
test -f src/backend/Libraries/Domain/Models/WorkflowStateLog.cs   || { echo "FAIL: WorkflowStateLog.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/Workflow/IWorkflowService.cs || { echo "FAIL: IWorkflowService.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/Workflow/WorkflowService.cs  || { echo "FAIL: WorkflowService.cs missing"; exit 1; }
test -f src/backend/API/Controllers/WorkflowController.cs         || { echo "FAIL: WorkflowController.cs missing"; exit 1; }
test -f src/frontend/main/src/types/workflow.ts                   || { echo "FAIL: workflow.ts missing"; exit 1; }
test -f src/frontend/main/src/services/workflowService.ts         || { echo "FAIL: workflowService.ts missing"; exit 1; }

grep -q "IWorkflowService" src/backend/API/Program.cs \
  || { echo "FAIL: IWorkflowService not registered in Program.cs"; exit 1; }
grep -q "EWorkflowState" src/frontend/main/src/types/workflow.ts \
  || { echo "FAIL: EWorkflowState mirror missing in frontend types"; exit 1; }

# Build
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check

echo "verify.sh: OK (workflow-engine feature wired)"
