## LLamaHub - Temporary Multi context implementation of LLamaSharp
- [Demo](https://llamaweb.chainstack.nz/)

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

