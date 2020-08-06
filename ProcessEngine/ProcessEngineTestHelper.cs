using System;
using System.Collections.Generic;
using System.IO;
using Camunda.Api.Client;
using Camunda.Api.Client.Deployment;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessInstance;

namespace Camunda.Test.Example.ProcessEngine
{
	public class ProcessEngineTestHelper
	{
		private CamundaClient CamundaClient;
		private List<string> DeploymentIds;
		private static readonly string TestWorkerId = "dot-net-test";
		public ProcessEngineTestHelper()
		{
			CamundaClient = CamundaClient.Create("http://localhost:8090/engine-rest");
		}

		public void DeployModel(string model)
		{
			DeployModels(new string[]{ model });
		}
		
		public void DeployModels(string[] models)
		{
			DeploymentIds = new List<string>();

			foreach(string model in models){
				Console.WriteLine($"Deploying model {model}");
				var deploymentId = CamundaClient.Deployments.Create("deployment", true, false, model, null,
								new ResourceDataContent(File.OpenRead("./bpmn/" + model), model)).Result.Id;
				Console.WriteLine($"Deployed model {deploymentId}");
				
				DeploymentIds.Add(deploymentId);
			}
		}

		public void CleanModels(){
			foreach(string deploymentId in DeploymentIds){
				Console.WriteLine($"Cleaning deployment {deploymentId}");
				CamundaClient.Deployments[deploymentId].Delete(true, true, true).Wait();
			}
		}

		public void CorrelateMessage(CorrelationMessage message){
			CamundaClient.Messages.DeliverMessage(message).Wait();
		}

		public List<ProcessInstanceInfo> QueryProcessInstances(ProcessInstanceQuery processInstanceQuery){
			return CamundaClient.ProcessInstances.Query(processInstanceQuery).List().Result;
		}

		public List<LockedExternalTask> FetchAndLockTasks(string topicName)
		{
			Console.WriteLine($"Executing fetch and lock for topic {topicName}");

			var fetchAndLockRequest = new FetchExternalTasks()
			{
				WorkerId = TestWorkerId,
				MaxTasks = 1,
				Topics = new List<FetchExternalTaskTopic>{
										new FetchExternalTaskTopic(topicName, 10000)
								}
			};

			List<LockedExternalTask> lockedExternalTasks = CamundaClient.ExternalTasks
								.FetchAndLock(fetchAndLockRequest).Result;

			Console.WriteLine($"Number of locked external tasks {lockedExternalTasks.Count}");

			return lockedExternalTasks;
		}
		public void RunExternalTasks(List<LockedExternalTask> lockedExternalTasks, Dictionary<string, VariableValue> variables)
		{
			Console.WriteLine($"Number of external tasks to be completed {lockedExternalTasks.Count}");

			foreach (LockedExternalTask lockedExternalTask in lockedExternalTasks)
			{
				var completeExternalTask = new CompleteExternalTask()
				{
					WorkerId = TestWorkerId,
					Variables = variables
				};
				CamundaClient.ExternalTasks[lockedExternalTask.Id].Complete(completeExternalTask).Wait();
				Console.WriteLine($"Completed external task {lockedExternalTask.Id}");
			}
		}


	}
}