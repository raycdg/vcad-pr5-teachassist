import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js"
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js"
import { ResourceTemplate } from "@modelcontextprotocol/sdk/server/mcp.js"
import { z } from "zod"

const server = new McpServer({ name: "dev-tools", version: "0.1.0" })


server.registerTool(
  "check_service_health",
  {
    description: "Проверить состояние сервиса по имени",
    inputSchema: { service: z.string() },
  },
  async ({ service }) => {
    const statuses = {
      api:   { status: "ok",       latency_ms: 42  },
      db:    { status: "ok",       latency_ms: 5   },
      cache: { status: "degraded", latency_ms: 380 },
    }
    const result = statuses[service]
    if (!result) 
      throw new Error(`Service '${service}' not found`)
    return { content: [{ type: "text", text: JSON.stringify(result) }] }
  }
)

server.registerResource(
  "env-config",
  new ResourceTemplate("config://{env}", { list: undefined }),
  { description: "Конфигурация для указанного окружения" },
  async (uri, { env }) => {
    const configs = {
      dev:  { debug: true,  db_host: "localhost" },
      prod: { debug: false, db_host: "db.internal" },
    }
    const config = configs[env]
    if (!config) 
      throw new Error(`Unknown environment: '${env}'`)
    return { contents: [{ uri: uri.href, text: JSON.stringify(config) }] }
  }
)

server.registerResource(
  "services-list",
  "config://services",
  { description: "Список всех зарегистрированных сервисов" },
  async (uri) => ({
    contents: [{ uri: uri.href, text: JSON.stringify(["api", "db", "cache", "worker"]) }],
  })
)

server.registerPrompt(
  "analyze_slow_query",
  {
    description: "Шаблон для анализа медленного SQL-запроса",
    argsSchema: { sql: z.string() },
  },
  ({ sql }) => ({
    messages: [{
      role: "user",
      content: {
        type: "text",
        text: `Проанализируй этот SQL-запрос и предложи оптимизации.\nУчти: индексы, N+1 проблемы, возможность использования CTE.\n\nЗапрос:\n${sql}`,
      },
    }],
  })
)

const transport = new StdioServerTransport()
await server.connect(transport)
