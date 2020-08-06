using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Camunda.Test.Example.ProcessEngine
{
	public class DockerProcessEngine
	{
		private DockerClient DockerClient;
		private string ContainerId;

		public DockerProcessEngine()
		{
			DockerClient = BuildDockerClient();
		}

		public void Start()
		{
			Console.WriteLine($"Building Camunda docker container");
			var progress = new Progress<JSONMessage>();
			DockerClient.Images.CreateImageAsync(
				new ImagesCreateParameters
				{
					FromImage = "camunda/camunda-bpm-platform:",
					Tag = "run-7.13.0",
				}, null, progress).Wait();

			ContainerId = DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
			{
				Name = "camunda-unit-test",
				Image = "camunda/camunda-bpm-platform:run-7.13.0",
				ExposedPorts = new Dictionary<string, EmptyStruct>
								{
												{
													"8080", default(EmptyStruct)
												},
												{
													"8000", default(EmptyStruct)
												},
												{
													"9404", default(EmptyStruct)
												}
								},
				HostConfig = new HostConfig
				{
					PortBindings = new Dictionary<string, IList<PortBinding>>
										{
														{"8080", new List<PortBinding> {new PortBinding {HostPort = "8090"}}},
														{"8000", new List<PortBinding> {new PortBinding {HostPort = "8000"}}},
														{"9404", new List<PortBinding> {new PortBinding {HostPort = "9404"}}}
										},
					PublishAllPorts = true
				}
			}).Result.ID;

			DockerClient.Containers.StartContainerAsync(ContainerId, null).Wait();

			var started = false;

			while (!started)
			{
				Thread.Sleep(5000);
				Stream result = DockerClient.Containers.GetContainerLogsAsync(ContainerId, new ContainerLogsParameters()
				{
					ShowStdout = true
				}).Result;
				StreamReader reader = new StreamReader(result);
				started = reader.ReadToEnd().Contains("starting to acquire jobs");
			}
		}

		public void Stop()
		{
			Console.WriteLine($"Stopping Camunda docker container");
			DockerClient.Containers.RemoveContainerAsync(ContainerId, new ContainerRemoveParameters()
			{
				RemoveVolumes = true,
				Force = true
			}).Wait();
		}

		private DockerClient BuildDockerClient()
		{
			string url;
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			if (isWindows)
			{
				url = "npipe://./pipe/docker_engine";
			}
			else
			{
				url = "unix:///var/run/docker.sock";
			}

			return new DockerClientConfiguration(new Uri(url)).CreateClient();
		}

	}
}