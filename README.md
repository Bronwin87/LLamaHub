## LLamaHub - Temporary Multi context implementation of LLamaSharp
- [Demo](https://llamaweb.chainstack.nz/)

This library give the ability to open multiple contexts on a single model using LLamaSharp


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
	InferenceParams inferenceParams = new InferenceParams() { Temperature = 0.6f, AntiPrompts = new List<string> { "User:" } }


	// Show the prompt
	System.Console.WriteLine();
	System.Console.Write(prompt);

	// Run the inference in a loop on context1
	while (prompt != "stop")
	{
		await foreach (var text in executor1.InferAsync(prompt, ))
		{
			System.Console.Write(text);
		}
		prompt = System.Console.ReadLine();
	}
```




## Websockets
Using signalr websockets simplifys the streaming of responses and model per connection management


## Setup
You can setup Models in the appsettings.json

**Models**
You can add multiple models to the options for quick selection in the UI, options are based on ModelParams so its fully configurable



Example:
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

## Web Demo

Prompt
![demo-ui](https://i.imgur.com/FG0YEzw.png)


Parameters
![demo-ui2](https://i.imgur.com/fZEQTQ5.png)

