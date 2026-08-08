import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import { installCpuBenchListener } from "./cpuBenchBootstrap";
import "./index.css";

installCpuBenchListener();

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
