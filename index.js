const express = require('express');
const axios = require('axios');
const puppeteer = require('puppeteer');
require('dotenv').config();
const { GoogleGenerativeAI } = require("@google/generative-ai");

const PORT = 3001;
const gitApi = 'https://api.github.com/';
const token = process.env.GIT_TOKEN;
const apiKey = process.env.API_KEY;

const genAI = new GoogleGenerativeAI(apiKey);

const app = express();
app.use(express.json());

app.post('/run', async (req, res) => {
    const { user, repo, type } = req.body;
    let instruction = "";
    if (type === "README") {
        instruction = "Write A Readme for the code. The Readme should include a brief summary of the code (including structures, features), the tech stack (Languages, Frameworks, Technologies), information about how to install dependencies, how to run the project locally, any configuration settings that need to be adjusted, instructions for testing, contributing guidelines, and licensing information. Make sure the format doesn't have any errors, especially the installation part.";
    } else if (type.charAt(0) === "B") {
        instruction = `Explain the code in ${parseInt(type.charAt(1), 10)} bullet points to write on resume and an extra line explaining the tech stack`;
    }

    try {
        const userExists = await checkUser(user);
        if (!userExists) return res.status(404).send('User not found');

        const repoExists = await checkRepo(user, repo);
        if (!repoExists) return res.status(404).send('Repository not found');

        const files = await listFiles(user, repo);
        if (!files) return res.status(400).json({ message: 'Failed to receive files' });

        let browser;
        try {
            browser = await puppeteer.launch({
                headless: false,
                args: ['--no-sandbox', '--disable-setuid-sandbox']
            });

            const page = await browser.newPage();
            await page.goto('https://chatgpt-prompt-splitter.jjdiaz.dev/');
            await configurePageForPromptSplit(page, files);

            const buttonText = await getButtonText(page, '#split-btn');
            const model = genAI.getGenerativeModel({ model: "gemini-1.5-flash" });

            if (buttonText === 'Prompt is shorter than split length') {
                await generateContentAndRespond(res, model, files, instruction);
            } else {
                await generateAndCombineContent(res, model, files, buttonText);
            }
        } catch (error) {
            if (browser) await browser.close();
            console.error('Error:', error);
            res.status(500).json({ message: 'Internal Server Error' });
        }
    } catch (error) {
        console.error('Error:', error);
        res.status(500).send('Internal server error');
    }
});

app.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});

async function checkUser(username) {
    try {
        const headers = { Authorization: `token ${token}` };
        const response = await axios.get(`${gitApi}users/${username}`, { headers });
        return response.status === 200;
    } catch (error) {
        return false;
    }
}

async function checkRepo(username, repo) {
    try {
        const headers = { Authorization: `token ${token}` };
        const response = await axios.get(`${gitApi}repos/${username}/${repo}`, { headers });
        return response.status === 200;
    } catch (error) {
        if (error.response && error.response.status === 404) {
            return false;
        }
        throw error;
    }
}

async function listFiles(username, repo, defaultPath = '') {
    const moduleManagement = [
        { language: "JavaScript (Node.js)", packageManager: "npm (Node Package Manager) or yarn", directory: "node_modules" },
        { language: "Python", packageManager: "pip", directory: "site-packages" },
        { language: "Java", packageManager: "Maven or Gradle", directory: "lib" },
        { language: "Ruby", packageManager: "RubyGems", directory: "Gem directory" },
        { language: "C/C++", packageManager: "Make, CMake, or Bazel", directory: "System directories or project directory" },
        { language: "Go", packageManager: "Modules", directory: "go.mod file" },
        { directory: "assets" }
    ];

    const supportedExtensions = [
        'html', 'css', 'js', 'jsx', 'ts', 'py', 'rb', 'java', 'kt', 'swift',
        'c', 'cpp', 'cs', 'go', 'php', 'sql', 'md', 'yaml', 'yml', 'sh', 'ps1',
        'bat', 'cmd', 'xml', 'svg', 'pl', 'rs', 'lua', 'coffee', 'sass', 'scss',
        'vue'
    ];

    async function fetchFileContent(username, repo, path) {
        try {
            const headers = { Authorization: `token ${token}` };
            const response = await axios.get(`${gitApi}repos/${username}/${repo}/contents/${path}`, { headers });
            if (response.status === 200) {
                const content = Buffer.from(response.data.content, 'base64').toString();
                return { name: path, content };
            } else {
                return null;
            }
        } catch (error) {
            return null;
        }
    }

    async function fetchFiles(username, repo, path) {
        try {
            const headers = { Authorization: `token ${token}` };
            const response = await axios.get(`${gitApi}repos/${username}/${repo}/contents/${path}`, { headers });
            if (response.status === 200) {
                const files = [];
                for (const item of response.data) {
                    if (item.type === 'file') {
                        const extension = item.name.split('.').pop();
                        if (supportedExtensions.includes(extension)) {
                            const fileContent = await fetchFileContent(username, repo, item.path);
                            if (fileContent) {
                                files.push(fileContent);
                            }
                        }
                    } else if (item.type === 'dir') {
                        const isModuleDirectory = moduleManagement.some(module => module.directory === item.name);
                        if (!isModuleDirectory) {
                            const nestedFiles = await fetchFiles(username, repo, `${path}${path ? '/' : ''}${item.name}`);
                            files.push(...nestedFiles);
                        }
                    }
                }
                return files;
            } else {
                return [];
            }
        } catch (error) {
            console.error('Error fetching files:', error);
            return [];
        }
    }

    const files = await fetchFiles(username, repo, defaultPath);
    return files.map(file => ({ name: file.name, content: file.content }));
}

async function configurePageForPromptSplit(page, files) {
    await page.select('#preset', 'custom');
    const inputSelector = "#split_length";
    await page.waitForSelector(inputSelector);
    await page.evaluate((selector) => {
        const input = document.querySelector(selector);
        input.value = input.value.replace(/\d/g, '');
        input.value = '750000';
    }, inputSelector);
    await page.waitForSelector('#prompt');
    await page.evaluate((files) => {
        document.querySelector('#prompt').value = JSON.stringify(files);
    }, files);
    await page.type('#prompt', " ");
}

async function getButtonText(page, selector) {
    await page.waitForSelector(selector);
    return await page.evaluate((selector) => {
        const button = document.querySelector(selector);
        return button ? button.textContent.trim() : '';
    }, selector);
}

async function generateContentAndRespond(res, model, files, instruction) {
    try {
        let combinedContent = '';
        for (const file of files) {
            const { name, content } = file;
            combinedContent += JSON.stringify(name) + JSON.stringify(content) + '\n';
        }
        const prompt = combinedContent + instruction;
        const result = await model.generateContent(prompt);
        const response = await result.response;
        const text = await response.text();
        res.status(200).send(text);
    } catch (error) {
        console.error('Error processing request:', error);
        res.status(500).json({ message: 'An error occurred while processing the data' });
    }
}

async function generateAndCombineContent(res, model, files, buttonText) {
    try {
        const numParts = parseInt(buttonText.match(/\d+/)[0], 10) + 1;
        const combinedContent = files.map(file => JSON.stringify(file.name) + JSON.stringify(file.content) + '\n').join('');
        const splitParts = splitCode(combinedContent, numParts);

        let combineResult = '';
        for (const part of splitParts) {
            try {
                const result = await model.generateContent(part + "write a short summary like in 100 words summary of code and the languages used here.");
                const response = await result.response;
                const text = await response.text();
                combineResult += text;
            } catch (error) {
                console.error("Error processing item:", error);
            }
            await new Promise(resolve => setTimeout(resolve, 10000)); // Waits for 10 seconds
        }

        const finalInstruction = "Here are the snippets of different parts of the code. Write a Readme for the code. The Readme should include a brief summary of the code (including structures, features), the tech stack (Languages, Frameworks, Technologies), information about how to install dependencies, how to run the project locally, any configuration settings that need to be adjusted, instructions for testing, contributing guidelines, and licensing information. Make sure the format doesn't have any errors, especially the installation part.";

        const finalPrompt = combineResult + finalInstruction
        const finalResult = await model.generateContent(finalPrompt);
        const finalResponse = await finalResult.response;
        const finalText = await finalResponse.text();
        res.status(200).send(finalText);
    } catch (error) {
        console.error('Error processing request:', error);
        res.status(500).json({ message: 'An error occurred while processing the data' });
    }
}

function splitCode(s, n) {
    const partLength = Math.floor(s.length / n);
    const remainder = s.length % n;

    const parts = new Array(n);
    let start = 0;

    for (let i = 0; i < n; i++) {
        let end = start + partLength + (i < remainder ? 1 : 0);
        parts[i] = s.slice(start, end);
        start = end;
    }

    return parts;
}
