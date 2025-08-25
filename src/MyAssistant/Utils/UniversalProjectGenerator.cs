using System.Text;
namespace MyAssistant.Utils
{

    public class UniversalProjectGenerator
    {
        /// <summary>
        /// 分析对话历史，识别项目类型和技术栈
        /// </summary>
        public static ProjectContext AnalyzeProjectContext(string userHistory)
        {
            var context = new ProjectContext();
            var history = userHistory.ToLower();

            // 识别编程语言
            if (history.Contains("c#") || history.Contains("csharp") || history.Contains(".net") ||
                history.Contains("namespace") || history.Contains("using system"))
            {
                context.Language = "csharp";
                context.Framework = ".NET";
            }
            else if (history.Contains("java") || history.Contains("spring") || history.Contains("maven"))
            {
                context.Language = "java";
                context.Framework = "Spring/Maven";
            }
            else if (history.Contains("python") || history.Contains("django") || history.Contains("flask"))
            {
                context.Language = "python";
                context.Framework = "Python";
            }
            else if (history.Contains("javascript") || history.Contains("node") || history.Contains("react"))
            {
                context.Language = "javascript";
                context.Framework = "Node.js/React";
            }
            else if (history.Contains("typescript") || history.Contains("angular") || history.Contains("vue"))
            {
                context.Language = "typescript";
                context.Framework = "TypeScript";
            }

            // 识别项目类型
            if (history.Contains("类库") || history.Contains("library") || history.Contains("sdk"))
            {
                context.ProjectType = "library";
            }
            else if (history.Contains("api") || history.Contains("服务") || history.Contains("service"))
            {
                context.ProjectType = "api";
            }
            else if (history.Contains("web") || history.Contains("网站") || history.Contains("前端"))
            {
                context.ProjectType = "web";
            }
            else if (history.Contains("控制台") || history.Contains("console") || history.Contains("cli"))
            {
                context.ProjectType = "console";
            }

            // 识别特定领域
            if (history.Contains("天气") || history.Contains("weather"))
            {
                context.Domain = "weather";
            }
            else if (history.Contains("用户") || history.Contains("user") || history.Contains("auth"))
            {
                context.Domain = "user-management";
            }
            else if (history.Contains("电商") || history.Contains("shop") || history.Contains("order"))
            {
                context.Domain = "ecommerce";
            }

            return context;
        }

        /// <summary>
        /// 生成通用的项目生成提示词
        /// </summary>
        public static string GeneratePrompt(string userHistory)
        {
            var context = AnalyzeProjectContext(userHistory);

            var prompt = new StringBuilder();

            prompt.AppendLine("你是一名专业的软件开发工程师，请根据以下对话历史生成一个完整的项目。");
            prompt.AppendLine();

            // 项目上下文
            prompt.AppendLine("**项目上下文分析：**");
            if (!string.IsNullOrEmpty(context.Language))
            {
                prompt.AppendLine($"- 编程语言：{context.Language}");
            }
            if (!string.IsNullOrEmpty(context.Framework))
            {
                prompt.AppendLine($"- 技术框架：{context.Framework}");
            }
            if (!string.IsNullOrEmpty(context.ProjectType))
            {
                prompt.AppendLine($"- 项目类型：{GetProjectTypeDescription(context.ProjectType)}");
            }
            if (!string.IsNullOrEmpty(context.Domain))
            {
                prompt.AppendLine($"- 业务领域：{context.Domain}");
            }
            prompt.AppendLine();

            // 通用格式要求
            prompt.AppendLine("**严格的格式要求：**");
            prompt.AppendLine("1. 必须使用以下格式返回每个文件：");
            prompt.AppendLine("   ## 文件路径（相对路径，不要以/开头）");
            prompt.AppendLine("   ```语言名");
            prompt.AppendLine("   文件内容");
            prompt.AppendLine("   ```");
            prompt.AppendLine();

            // 示例格式
            prompt.AppendLine("2. 示例格式：");
            prompt.AppendLine(GetExampleFormat(context));
            prompt.AppendLine();

            // 项目结构要求
            prompt.AppendLine("3. 项目结构要求：");
            prompt.AppendLine(GetStructureRequirements(context));
            prompt.AppendLine();

            // 文件路径格式
            prompt.AppendLine("4. 文件路径格式：");
            prompt.AppendLine("   - 使用相对路径：src/models/User.cs");
            prompt.AppendLine("   - 不要使用绝对路径：/src/models/User.cs");
            prompt.AppendLine("   - 保持目录结构清晰和合理");
            prompt.AppendLine();

            // 代码质量要求
            prompt.AppendLine("5. 代码质量要求：");
            prompt.AppendLine("   - 代码要完整可运行，不要使用占位符");
            prompt.AppendLine("   - 包含必要的导入/引用语句");
            prompt.AppendLine("   - 遵循语言的最佳实践和编码规范");
            prompt.AppendLine("   - 添加适当的注释和文档");
            prompt.AppendLine();

            prompt.AppendLine("**对话历史：**");
            prompt.AppendLine(userHistory);
            prompt.AppendLine();
            prompt.AppendLine("请根据上述要求和对话历史，生成完整的项目结构和代码。");

            return prompt.ToString();
        }

        private static string GetProjectTypeDescription(string projectType)
        {
            return projectType switch
            {
                "library" => "类库/SDK项目",
                "api" => "API服务项目",
                "web" => "Web应用项目",
                "console" => "控制台应用项目",
                _ => "通用项目"
            };
        }

        private static string GetExampleFormat(ProjectContext context)
        {
            return context.Language switch
            {
                "csharp" => @"   ## src/Models/WeatherData.cs
   ```csharp
   using System;
   
   namespace MyProject.Models
   {
       public class WeatherData
       {
           public string Temperature { get; set; }
       }
   }
   ```",

                "java" => @"   ## src/main/java/com/example/models/WeatherData.java
   ```java
   package com.example.models;
   
   public class WeatherData {
       private String temperature;
       
       public String getTemperature() {
           return temperature;
       }
   }
   ```",

                "python" => @"   ## src/models/weather_data.py
   ```python
   from dataclasses import dataclass
   
   @dataclass
   class WeatherData:
       temperature: str
   ```",

                "javascript" => @"   ## src/models/WeatherData.js
   ```javascript
   class WeatherData {
       constructor(temperature) {
           this.temperature = temperature;
       }
   }
   
   module.exports = WeatherData;
   ```",

                _ => @"   ## src/models/WeatherData.ext
   ```
   // 文件内容根据具体语言编写
   ```"
            };
        }

        private static string GetStructureRequirements(ProjectContext context)
        {
            var requirements = new StringBuilder();
            requirements.AppendLine(" - 方法、属性、类、接口必须添加相应注释");
            switch (context.Language)
            {
                case "csharp":
                    requirements.AppendLine("   - 包含.csproj项目文件");
                    if (context.ProjectType == "library")
                    {
                        requirements.AppendLine("   - 类库项目使用<OutputType>Library</OutputType>");
                        requirements.AppendLine("   - 不生成Program.cs文件");
                    }
                    requirements.AppendLine("   - 保持命名空间一致性");
                    requirements.AppendLine("   - 使用合适的目录结构（Models, Services, Utils等）");
                    break;

                case "java":
                    requirements.AppendLine("   - 包含pom.xml或build.gradle构建文件");
                    requirements.AppendLine("   - 遵循Maven/Gradle标准目录结构");
                    requirements.AppendLine("   - 保持包名一致性");
                    break;

                case "python":
                    requirements.AppendLine("   - 包含requirements.txt依赖文件");
                    requirements.AppendLine("   - 包含setup.py或pyproject.toml配置文件");
                    requirements.AppendLine("   - 遵循Python包结构规范");
                    break;

                case "javascript":
                case "typescript":
                    requirements.AppendLine("   - 包含package.json配置文件");
                    requirements.AppendLine("   - 合理组织src目录结构");
                    if (context.Language == "typescript")
                    {
                        requirements.AppendLine("   - 包含tsconfig.json配置文件");
                    }
                    break;

                default:
                    requirements.AppendLine("   - 包含适当的项目配置文件");
                    requirements.AppendLine("   - 遵循语言的标准目录结构");
                    break;
            }

            requirements.AppendLine("   - 包含README.md文档文件");
            requirements.AppendLine("   - 代码结构清晰，职责分离");

            return requirements.ToString();
        }
    }

    public class ProjectContext
    {
        public string Language { get; set; } = "";
        public string Framework { get; set; } = "";
        public string ProjectType { get; set; } = "";
        public string Domain { get; set; } = "";
    }
}
