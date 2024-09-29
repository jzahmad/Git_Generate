
## ReadMeGenie

This code demonstrates an implementation of a RESTful API controller that can generate content (specifically, a ReadMe file) based on provided code snippets. The controller utilizes the Gemini model from Google AI to perform the content generation task.

**RunController.cs:**
 - Defines the `RunController` class, which handles HTTP POST requests at the `/Run` endpoint.
 - In the `Run` method, it receives a `Request` object, which contains information about the user, repository name, and type of content to be generated.
 - It first checks if the user exists and the repository exists.
 - Then, it retrieves the list of files from the repository and combines their content into a single string.
 - It checks the total length of the combined content:
   - If it's less than 75000 characters, it generates the content using a single prompt.
   - If it's over 75000 characters, it splits the content into multiple parts, generates content for each part separately, and then combines the results.
 - Finally, it returns the generated content as a string.

**FileContent.cs:**
 - Represents a file's name and content.

**ModuleManagement.cs:**
 - Represents information about module management tools (e.g., package managers) used in different programming languages.

**Request.cs:**
 - Represents the request data received by the controller.

**Program.cs:**
 - Configures the ASP.NET Core application and adds support for AWS Lambda hosting with the HttpApi event source.
 - Defines the HTTP pipeline and maps the `Run` controller's endpoint.

**Usage:**
- The API can be used to generate a ReadMe file by sending a POST request to the `/Run` endpoint with the following JSON payload:
```json
{
  "Type": "<type of content to generate>",
  "Name": "<repository name>",
  "User": "<username>"
}
```
- The API responds with the generated ReadMe file in text format.
