import { type Plugin, tool } from "@opencode-ai/plugin"

export const NvimEditPlugin: Plugin = async ({ $, directory }) => {
  const editedFiles: string[] = []

  return {
    event: async ({ event }) => {
      if (event.type === "file.edited") {
        const filePath = event.properties?.file as string | undefined
        if (filePath && !editedFiles.includes(filePath)) {
          editedFiles.push(filePath)
        }
      }
    },

    tool: {
      "nvim-edit": tool({
        description: "Open a file in nvim in a new tmux window. Lists edited files if no file is specified.",
        args: {
          file: tool.schema.string().optional().describe("File path or index from the edited files list"),
        },
        async execute(args) {
          if (!args.file) {
            if (editedFiles.length === 0) {
              return "No files have been edited yet. Specify a file path to open."
            }
            const list = editedFiles.map((f, i) => `  ${i}: ${f}`).join("\n")
            return `Edited files:\n${list}\n\nUse "nvim-edit <path or index>" to open a file.`
          }

          let filePath: string
          const index = parseInt(args.file, 10)
          if (!isNaN(index) && index >= 0 && index < editedFiles.length) {
            filePath = editedFiles[index]
          } else {
            filePath = args.file.startsWith("/") ? args.file : `${directory}/${args.file}`
          }

          try {
            await $`tmux new-window -n "nvim" nvim ${filePath}`
            return `Opened ${filePath} in nvim (new tmux window)`
          } catch (err) {
            return `Failed to open nvim: ${err instanceof Error ? err.message : String(err)}`
          }
        },
      }),
    },
  }
}
