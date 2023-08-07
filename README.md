# LLamaHub


## Overview

Welcome to LLamaHub! This repository serves as a collection of multiple UI & API projects related to the LLama project. Here, you'll find various user interface applications, including Web, API, WPF, and Websocket, all built to showcase the capabilities of the LLama project.

LLamaHub is built on top of the popular LLamaSharp and llama.cpp projects, extending their functionalities with a range of user-friendly UI applications. LLamaSharp is a powerful library that provides C# interfaces and abstractions for the popular llama.cpp, the C++ counterpart that offers high-performance inference capabilities on low end hardware. LLamaHub complements these projects by creating intuitive UI & API interfaces, making the power of LLamaSharp and llama.cpp more accessible to users.


## Multiple Context Implemetation
```cs
var modelPath = "<Your model path>"; // change it to your own model path
var prompt = "Below is an instruction that describes a task. Write a response that appropriately completes the request."; 

// Create Model
LLamaHubModel model = new LLamaHubModel(new ModelParams(modelPath, contextSize: 1024, seed: 1337, gpuLayerCount: 5));

// Create Context
LLamaHubModelContext context1 = await model.CreateContext("context1");

// Create executor
LLamaHubInteractiveExecutor executor1 = new LLamaHubInteractiveExecutor(context1);

// Inference Parameters
IInferenceParams inferenceParams = new InferenceParams() { Temperature = 0.6f, AntiPrompts = new List<string> { "User:" } }


// Show the prompt
System.Console.WriteLine();
System.Console.Write(prompt);

// Run the inference in a loop on context1
while (prompt != "stop")
{
	await foreach (var text in executor1.InferAsync(prompt, inferenceParams))
	{
		System.Console.Write(text);
	}
	prompt = System.Console.ReadLine();
}
```


## Projects

Here are the UI projects included in LLamaHub:

1. **LLamaWeb**: ASP.NET Core Web interface with all the base functions of llama.cpp & LLamaSharp
[WebInterface Demo](https://llamaweb.chainstack.nz/)

2. **LLamaAPI**: ASP.NET Core WebAPI implemntation with all the base functions of llama.cpp & LLamaSharp

3. **LLamaWPF**: WPF UI interface with all the base functions of llama.cpp & LLamaSharp

4. **LLamaSignalr**: Signale websocket server and cliet implemetations for use in web and .NET environments




## Getting Started

To get started with a specific UI project, please refer to the README file of each project located in their respective directories.

## Setup
You can setup Models in the appsettings.json

```json
	{
	  "Logging": {
		"LogLevel": {
		  "Default": "Information",
		  "Microsoft.AspNetCore": "Warning"
		}
	  },
	  "AllowedHosts": "*",
	  "LLamaHubConfig": {
		"Models": [
		  {
			"Name": "WizardLM-7B",
			"MaxInstances": 2,
			"ModelPath": "D:\\Repositories\\AI\\Models\\wizardLM-7B.ggmlv3.q4_0.bin",
			"ContextSize": 2048
		  }
		]
	  }
	}
```



## Contribution

We welcome contributions to LLamaHub! If you have any ideas, bug reports, or improvements, feel free to open an issue or submit a pull request.

## License

This project is licensed under the terms of the MIT license.
