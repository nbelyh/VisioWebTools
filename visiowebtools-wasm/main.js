import { dotnet } from "./dotnet.js";

window.VisioWebToolsDotNet = {

  load: async () => {
    const { getAssemblyExports, getConfig } = await dotnet
      .withDiagnosticTracing(false)
      .create();
  
    const config = getConfig();
    return await getAssemblyExports(config.mainAssemblyName);
  }
};