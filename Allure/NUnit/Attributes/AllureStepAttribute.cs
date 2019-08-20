﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using Noksa.Allure.StepInjector.Abstract;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    public class AllureStepAttribute : AbstractAllureStepAttribute
    {

        private int AssertsBeforeCount { get; set; }

        public AllureStepAttribute()
        {
            SetAssertsBefore();
        }
        
        public AllureStepAttribute(string stepText) : base(stepText)
        {
            SetAssertsBefore();
        }

        public override void OnEnter(MethodBase method, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(StepText)) StepText = method.Name;
            else ConvertParameters(parameters);
            AllureLifecycle.Instance.StartStep(StepText, StepUuid);
            if (parameters != null && parameters.Count > 0) ReportHelper.AddStepParameters(parameters.Select(o => o.Value).ToArray(), StepUuid);
            
        }

        public override void OnExit()
        {
            AllureLifecycle.Instance.UpdateStep(StepUuid, step => step.status = Status.passed);
            AllureLifecycle.Instance.StopStep(StepUuid);
        }

        public override void OnException(Exception e)
        {
            var stepStatus = Status.failed;
            Exception throwedEx;
            if (e is TargetInvocationException)
                throwedEx = AllureLifecycle.GetInnerExceptions(e)
                    .First(q => q.GetType() != typeof(TargetInvocationException));
            else
                throwedEx = e;

            if (throwedEx is InconclusiveException)
                stepStatus = Status.skipped;

            var list =
                TestContext.CurrentContext.Test.Properties.Get(AllureConstants.TestAsserts) as List<Exception>;
            list?.Add(throwedEx);
            if (!throwedEx.Data.Contains("Rethrow"))
            {
                throwedEx.Data.Add("Rethrow", true);
                AllureLifecycle.Instance.MakeStepWithExMessage(AssertsBeforeCount, StepText, throwedEx, stepStatus);
                AllureLifecycle.CurrentTestActionsInException?.ForEach(action => action.Invoke());
                AllureLifecycle.GlobalActionsInException?.ForEach(action => action.Invoke());
            }

            AllureLifecycle.Instance.UpdateStep(StepUuid, step => step.status = stepStatus);
            AllureLifecycle.Instance.StopStep(StepUuid);
        }


        private void SetAssertsBefore()
        {
            AssertsBeforeCount = TestContext.CurrentContext.Result.Assertions.Count();
        }
        
    }
}