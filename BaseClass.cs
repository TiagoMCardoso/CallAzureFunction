using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace TMC.Workflows
{
    public abstract class BaseClass : CodeActivity
    {
        public IOrganizationService service { get; private set; }
        public CodeActivityContext context { get; private set; }
        public IWorkflowContext workflowContext { get; private set; }
        public IOrganizationServiceFactory serviceFactory { get; private set; }
        public ITracingService tracingService { get; private set; }
        public string className { get; private set; }

        [MTAThread]
        protected override void Execute(CodeActivityContext codeActivityContext)
        {
            className = GetType().Name;

            context = codeActivityContext;
            workflowContext = context.GetExtension<IWorkflowContext>();
            serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            tracingService = context.GetExtension<ITracingService>();
            service = serviceFactory.CreateOrganizationService(workflowContext.UserId);

            this.ExecuteWorkflow();
        }

        public abstract void ExecuteWorkflow();
    }
}
