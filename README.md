## GitHub Readme Generator

This code implements a web service that generates a README file for a given GitHub repository. The service utilizes Google Generative AI's Gemini model and Puppeteer to process and analyze the repository's code and create a comprehensive README file with essential information.

### Features

* **Generates READMEs for various types of repositories:** The service can generate READMEs for codebases of varying complexity, including those with extensive code, diverse technologies, and different types of files.
* **Tailored README generation:** The service can generate READMEs based on specific user requirements, such as the need for a specific number of bullet points for resume purposes.
* **Efficient code analysis:** The service leverages Puppeteer to streamline the code analysis process, effectively parsing and splitting the code for further processing.
* **Integration with GitHub API:** The service interacts with the GitHub API to retrieve information about repositories and their contents, enabling accurate and up-to-date analysis.
* **Customization options:** Users can customize the generated README by specifying the desired number of bullet points for code summaries.

### Tech Stack

* **Node.js:** Server-side runtime environment
* **Express:** Web framework for building RESTful APIs
* **Axios:** HTTP client for making API requests
* **Puppeteer:** Headless Chrome browser for automated tasks
* **Google Generative AI:** AI model for text generation
* **dotenv:** Library for loading environment variables
* **GitHub API:** Interface for accessing repository data

### Installation and Setup

1. **Install Node.js and npm (or yarn):** [https://nodejs.org/](https://nodejs.org/)
2. **Clone the repository:** `git clone [repository URL]`
3. **Install dependencies:** `npm install`
4. **Create a `.env` file in the root directory:**
   ```
   GIT_TOKEN=[YOUR_GITHUB_TOKEN]
   API_KEY=[YOUR_GOOGLE_GENERATIVE_AI_API_KEY]
   ```
5. **Replace placeholders with your actual GitHub API token and Google Generative AI API key.**
6. **Start the server:** `npm start`

The server will be running on port 3001 (http://localhost:3001).

### Usage

To generate a README file, send a POST request to the `/run` endpoint with the following data in the request body:

```json
{
  "user": "username",
  "repo": "reponame",
  "type": "README" // or "B[number]" for bullet point summary
}
```

Replace `username`, `reponame`, and `type` with the actual values.

The response will contain the generated README text.

### Example

```
curl -X POST -H "Content-Type: application/json" \
  -d '{"user": "username", "repo": "reponame", "type": "README"}' \
  http://localhost:3001/run
```

### Contributing

Contributions are welcome! Please open an issue or submit a pull request if you have any suggestions or improvements.

