using System;
using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessInstance;
using Camunda.Test.Example.ProcessEngine;
using NUnit.Framework;

namespace Camunda.Test.Example
{
	[TestFixture]
	public class ProcessUnitTest
	{
		private ProcessEngineTestHelper testHelper;
		private DockerProcessEngine dockerProcessEngine;

		/* Initiliazing the Camunda Client and deploying bpmns*/
		[OneTimeSetUp]
		public void globalInitialize()
		{
			dockerProcessEngine = new DockerProcessEngine();
			dockerProcessEngine.Start();
		}

		[SetUp]
		public void initialize()
		{
			testHelper = new ProcessEngineTestHelper();
			testHelper.DeployModel("payment-process.bpmn");
		}

		[TearDown]
		public void cleanup()
		{
			testHelper.CleanModels();
		}

		[OneTimeTearDown]
		public void globalCleanup()
		{
			dockerProcessEngine.Stop();
		}

		[Test]
		public void TestHappyPath()
		{
			/* Creating the message and sending it to Camunda */
			var message = new CorrelationMessage()
			{
				MessageName = "MessagePaymentRequested",
				BusinessKey = Guid.NewGuid().ToString()
			};

			message.ProcessVariables
					.Set("paymentAmount", 10000)
					.Set("error", false)
					.Set("fail", false)
					.Set("resolvable", true);

			testHelper.CorrelateMessage(message);

			List<LockedExternalTask> chargeCreditTasks = testHelper.FetchAndLockTasks("charge-credit");
			Assert.AreEqual(chargeCreditTasks.Count, 1);

			Dictionary<string, VariableValue> variables = new Dictionary<string, VariableValue>();
			variables.Set("remainingAmount", 0);

			testHelper.RunExternalTasks(chargeCreditTasks, variables);

			List<LockedExternalTask> paymentFinishedTasks = testHelper.FetchAndLockTasks("payment-finished");
			testHelper.RunExternalTasks(paymentFinishedTasks, null);

			List<ProcessInstanceInfo> runningInstances = testHelper.QueryProcessInstances(new ProcessInstanceQuery()
			{
				ProcessDefinitionKey = "PaymentProcess"
			});

			Assert.AreEqual(runningInstances.Count, 0);
		}
	}
}