using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mscc.GenerativeAI;
using ReadMeGenie.Data;

namespace ReadMeGenie.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RunController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string gitUrl = "";
        private readonly string gitToken = "";
        private static readonly List<string> supportedExtensions = new List<string>
        {
            "html", "css", "js", "jsx", "ts", "py", "rb", "java", "kt", "swift",
            "c", "cpp", "cs", "go", "php", "sql", "md", "yaml", "yml", "sh", "ps1",
            "bat", "cmd", "xml", "svg", "pl", "rs", "lua", "coffee", "sass", "scss", "vue"
        };

        private static readonly List<ModuleManagement> moduleManagement = new List<ModuleManagement>
        {
            new ModuleManagement("JavaScript (Node.js)", "npm (Node Package Manager) or yarn", "node_modules"),
            new ModuleManagement("Python", "pip", "site-packages"),
            new ModuleManagement("Java", "Maven or Gradle", "lib"),
            new ModuleManagement("Ruby", "RubyGems", "Gem directory"),
            new ModuleManagement("C/C++", "Make, CMake, or Bazel", "System directories or project directory"),
            new ModuleManagement("Go", "Modules", "go.mod file"),
            new ModuleManagement("", "", "assets")
        };

        private readonly string GeminiToken = "AIzaSyD-QSmxChdGnNUbQQMFcktpRSj26YZsgNI";

        public RunController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> Run([FromBody] Request request)
        {

            string instruction;
            bool userExists = await CheckUserAsync(request.User);
            if (!userExists)
            {
                return NotFound("User not found");
            }


            bool repoExist = await CheckRepo(request.User, request.Name);
            if (!repoExist)
            {
                return NotFound("Repository not found");
            }


            var files = await ListFilesAsync(request.User, request.Name, "");
            if (request.Type == "ReadMe")
            {
                instruction = "Write A really lengthy Readme for the code. The Readme should include a briefly summary " +
               "summary of the code (including structures, explain every technical and non technical features), the tech stack " +
               "(Languages, Frameworks, Technologies), information about how to install dependencies, " +
               "how to run the project locally, any configuration settings that need to be adjusted, instructions for testing, contributing guidelines, and licensing information. Make sure the format doesn't have any errors, especially the installation part.";
            }
            else if (request.Type.Substring(0, 1).Equals('B'))

            {
                Console.WriteLine("lol");
                instruction = $"Explain the code in {int.Parse(request.Type[1].ToString())} bullet points to write on resume and an extra line explaining the tech stack";
            }
            else
            {
                instruction = request.Type;
            }

            string resultText;


            var buttonText = GetButtonText(files);
            // return Ok(buttonText);
            if (buttonText == "Prompt is shorter than split length")
            {
                resultText = await GenerateContent(files, instruction);
            }
            else
            {
                resultText = await GenerateAndCombineContent(files, buttonText, instruction);
            }

            return Ok(resultText);
        }

        private async Task<bool> CheckUserAsync(string user)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{gitUrl}/users/{user}");
                request.Headers.Add("Authorization", $"Bearer {gitToken}");
                request.Headers.Add("User-Agent", "request");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckRepo(string username, string repo)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{gitUrl}/repos/{username}/{repo}");
                request.Headers.Add("Authorization", $"Bearer {gitToken}");
                request.Headers.Add("User-Agent", "request");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<FileContent>> ListFilesAsync(string username, string repo, string defaultPath = "")
        {
            var files = await FetchFilesAsync(username, repo, defaultPath);
            var result = new List<FileContent>();

            foreach (var file in files)
            {
                result.Add(new FileContent { Name = file.Name, Content = file.Content });
            }

            return result;
        }

        private async Task<List<FileContent>> FetchFilesAsync(string username, string repo, string path)
        {
            var files = new List<FileContent>();
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", gitToken);

                var response = await _httpClient.GetAsync($"{gitUrl}/repos/{username}/{repo}/contents/{path}");

                if (response.IsSuccessStatusCode)
                {
                    var json = JArray.Parse(await response.Content.ReadAsStringAsync());

                    foreach (var item in json)
                    {
                        if (item["type"].ToString() == "file")
                        {
                            string fileName = item["name"].ToString();
                            string extension = fileName.Substring(fileName.LastIndexOf('.') + 1);

                            if (supportedExtensions.Contains(extension))
                            {
                                var fileContent = await FetchFileContentAsync(username, repo, item["path"].ToString());
                                if (fileContent != null)
                                {
                                    files.Add(new FileContent { Name = item["path"].ToString(), Content = fileContent });
                                }
                            }
                        }
                        else if (item["type"].ToString() == "dir")
                        {
                            bool isModuleDirectory = moduleManagement.Exists(module => module.Directory == item["name"].ToString());
                            if (!isModuleDirectory)
                            {
                                var nestedFiles = await FetchFilesAsync(username, repo, $"{path}{(string.IsNullOrEmpty(path) ? "" : "/")}{item["name"]}");
                                files.AddRange(nestedFiles);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching files: {ex.Message}");
            }

            return files;
        }

        private async Task<string?> FetchFileContentAsync(string username, string repo, string path)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{gitUrl}/repos/{username}/{repo}/contents/{path}");
                if (response.IsSuccessStatusCode)
                {
                    var contentJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                    string contentBase64 = contentJson["content"].ToString();
                    byte[] data = Convert.FromBase64String(contentBase64);
                    return Encoding.UTF8.GetString(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching file content: {ex.Message}");
            }

            return null;
        }


        private static string GetButtonText(List<FileContent> files)
        {
            // Calculate the total content length and return button text based on it
            int totalLength = files.Sum(f => f.Content.Length);
            if (totalLength < 75000)
            {
                return "Prompt is shorter than split length";
            }

            return totalLength.ToString();
        }

        private async Task<String> GenerateContent(List<FileContent> files, string instruction)
        {

            // Combine file content into a single prompt
            var combinedContent = new StringBuilder();
            foreach (var file in files)
            {
                combinedContent.AppendLine($"File Name: {file.Name}");
                combinedContent.AppendLine($"File Content: {file.Content}");
                combinedContent.AppendLine();
            }

            string prompt = combinedContent.ToString() + instruction;
            var requestData = new
            {
                prompt = new { text = prompt }
            };
            var contentString = JsonConvert.SerializeObject(requestData);

            string response = await GenerateContentFromModel(contentString, GeminiToken);

            return response;

        }

        private async Task<string> GenerateContentFromModel(string contentString, string token)
        {
            try
            {
                var googleAI = new GoogleAI(token);
                var model = googleAI.GenerativeModel(Model.GeminiPro);

                var response = await model.GenerateContent(contentString);

                string generatedText = response.Text;
                return generatedText;
            }
            catch (Exception ex)
            {
                return $"Error generating content {ex.Message}";
            }
        }

        private async Task<string> GenerateAndCombineContent(List<FileContent> files, string buttonText, string instruction)
        {

            string split_instruction = "write a short summary summary of code and the languages used here.";
            string finalInstruction =
                "Here are the snippets of different parts of the code." + instruction;

            int numParts = (int.Parse(buttonText) / 74000) + 1;

            var combinedContent = string.Join("", files.Select(file =>
                JsonConvert.SerializeObject(file.Name) + JsonConvert.SerializeObject(file.Content) + "\n"));

            var splitParts = SplitCode(combinedContent, numParts);
            var combineResult = new StringBuilder();

            foreach (var part in splitParts)
            {
                try
                {
                    var result = await GenerateContentFromModel(part + split_instruction, GeminiToken);
                    combineResult.Append(result);
                    System.Threading.Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing item: {ex.Message}");
                }
            }

            string finalPrompt = combineResult.ToString() + finalInstruction;
            var finalResult = await GenerateContentFromModel(finalPrompt, GeminiToken);
            return finalResult;
        }


        private List<string> SplitCode(string s, int n)

        {
            int maxLength = 74000;
            var parts = new List<string>();
            int start = 0;

            while (start < s.Length)
            {
       
                int length = Math.Min(maxLength, s.Length - start);
                parts.Add(s.Substring(start, length));
                start += length;
            }

            return parts;
        }
    }
}

