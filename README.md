# 📘 BUSINESS REQUIREMENTS DOCUMENT (BRD)

## TMT Interactive Analytics Dashboard & Intelligent Forecasting Agent

---

## 1. Executive Summary

The TMT Interactive Dashboard & Intelligent Forecasting Agent is an AI-enhanced analytics platform designed for Telecom, Media, and Technology (TMT) organizations. It combines interactive visualizations, natural language querying, automated metric discovery, and predictive forecasting to empower business users with rapid, actionable insights.

**Core Technology Stack:**
- **Frontend:** Angular 19 for responsive, dynamic UI components.
- **Backend:** FastAPI (Python) for scalable APIs and orchestration.
- **AI Layer:** Dual LangGraph pipelines for global and component-specific reasoning.
- **Data Layer:** Mock database (CSV/JSON/SQLite) for MVP, with extensible architecture for real-world sources (e.g., Databricks, Snowflake).

**Key Features:**
1. **Global Chat Assistant:** Cross-metric queries, multi-step analysis, and KPI recommendations.
2. **Component-Level Chat:** Contextual insights tied to individual charts or metrics.
3. **Dynamic Metrics Discovery:** Automated schema introspection to generate KPIs from database columns—no hardcoded metrics.
4. **Data Ingestion UI:** Frontend-only design for future data pipeline configuration.

This platform bridges the gap between raw data and strategic decision-making, enabling "what-if" scenarios without reliance on data science teams. It scales seamlessly from mock data to enterprise-grade integrations.

---

## 2. Business Goals

- **Democratize Data Access:** Allow operations, strategy, and executive teams to explore metrics independently, reducing dependency on data engineers.
- **Automate Insight Generation:** Dynamically identify and prioritize relevant KPIs based on database schema, tailored to TMT contexts (e.g., subscriber growth, ad revenue, network load).
- **Enable Predictive Planning:** Deliver short-term forecasts and scenario simulations for outcomes like churn, revenue, or capacity needs.
- **Accelerate Decision Cycles:** Support real-time business reviews, anomaly detection, and trend analysis via conversational interfaces.
- **Future-Proof Scalability:** Design for easy integration with cloud data warehouses, ensuring long-term adaptability.

**Target Outcomes:**
- 50% reduction in time-to-insight for TMT analytics.
- Increased adoption of forecasting tools for proactive resource allocation.

---

## 3. Project Scope

### 3.1 In Scope

#### A. Interactive Dashboard
- Dynamic rendering of visualizations (e.g., time-series lines, bar charts, comparisons, tables).
- Auto-generated metric cards based on discovered schema.
- User-applied filters (date ranges, segments like region or plan type).
- Export options for charts and data (CSV/PDF).

#### B. Global Chat Assistant
- Full natural language processing for dashboard-wide queries.
- Cross-metric integration (e.g., correlating subscriber trends with revenue).
- Multi-step reasoning: Query parsing → Metric selection → Analysis/Forecast → Visualization/Summary.
- Proactive KPI recommendations (e.g., "Top 5 metrics for Q3 forecasting").

**Example Query:** "Show projected subscriber numbers for Q3 if churn holds steady, and compare to revenue impacts."

#### C. Component-Level Chat
- Embedded chat widgets per chart/metric for focused interactions.
- Localized context: Trends, anomalies, explanations, and micro-forecasts.
- Lightweight responses to avoid global overload.

**Example Query:** "Why the spike in off-peak network usage last month?"

#### D. Dynamic Metrics Discovery
- Backend schema introspection to auto-generate metrics from DB columns.
- Classification: Numeric (e.g., counts, values), temporal (dates), categorical (segments).
- Agent-driven selection: Relevancy scoring for query alignment.
- No manual KPI configuration—system adapts to new data.

#### E. Forecasting & Scenario Modeling
- Built-in models: Linear regression, moving averages for baseline projections.
- Scenario adjustments: Numeric tweaks (e.g., +2% churn) with comparative visualizations.
- Overlay forecasts on charts (e.g., confidence intervals).
- TMT-tuned defaults: Subscriber churn, ARPU shifts, load balancing.

**Example Query:** "Forecast ad spend if audience engagement drops 10%—show vs. baseline."

#### F. Data Ingestion UI (Frontend-Only)
- File upload for mock data (CSV/Excel) with preview and validation.
- Configuration forms for future connectors: PostgreSQL, Databricks, Snowflake, AWS Redshift.
- Schema mapping interface: Column-to-metric assignments.
- Simulated workflows with success/error messaging—no backend processing.

#### G. Backend APIs (FastAPI)
- `/metrics/discover`: Schema introspection and KPI generation.
- `/metrics/query`: Dynamic SQL/filtered data retrieval.
- `/forecast`: Projection endpoints with parameters.
- `/scenario`: What-if simulations.
- `/chat/global` and `/chat/component`: Pipeline orchestration.

#### H. LangGraph AI Pipelines
- **Global Pipeline:** Orchestrates tools for multi-metric reasoning, recommendations, and end-to-end responses.
- **Component Pipeline:** Simplified chain for single-metric tasks (e.g., anomaly detection, local trends).

#### I. Data Layer
- Mock DB with sample TMT datasets (e.g., telecom subscribers, media ratings).
- Schema auto-generation for introspection.
- Extensible queries via SQLAlchemy.

### 3.2 Out of Scope (MVP)
- Full data ingestion backend (UI design only).
- Real-time data streaming or event-driven updates.
- Advanced ML models (e.g., Prophet, ARIMA, neural networks).
- Security features: RBAC, data governance, multi-tenancy.
- Cross-platform federation or ETL pipelines.
- Mobile/responsive optimizations beyond desktop.
- Custom visualizations (e.g., heatmaps, Sankey diagrams).

---

## 4. Stakeholders

| Role          | Responsibilities |
|---------------|------------------|
| **Product Owner** | Define requirements, prioritize features, validate acceptance criteria. |
| **Engineering Team** | Develop frontend, backend, AI pipelines, and integrations. |
| **Data Team** | Review schema designs, provide mock/real data samples (post-MVP). |
| **Business Users** | Test usability, provide feedback on TMT-specific scenarios. |
| **DevOps** | Handle deployment, CI/CD setup (future phases). |

---

## 5. High-Level Use Cases

### 5.1 Global Insights & Exploration
- User queries dashboard-wide trends (e.g., "Compare prime-time vs. off-peak usage over 6 months").
- System recommends KPIs and generates composite views.
- Export multi-metric reports for reviews.

### 5.2 Component-Level Analysis
- Hover/chat on a chart for drill-down (e.g., "Break down this churn spike by region").
- Localized forecasts with segment filters applied.

### 5.3 Scenario Planning
- Adjust variables (e.g., "Simulate IoT load if device adoption rises 15%").
- Visualize baselines vs. scenarios with overlaid projections.

### 5.4 Data Ingestion (Simulated)
- Upload mock CSV → Preview schema → Map columns → "Connect" to mock DB.
- Transition to real connector configs in future iterations.

### 5.5 TMT-Specific Flows
- **Telecom:** Forecast network capacity based on usage trends.
- **Media:** Project audience ratings and ad inventory.
- **Tech/SaaS:** Model ARR growth and feature adoption.

---

## 6. Functional Requirements

### 6.1 Metrics Discovery
- Introspect DB tables to output JSON schema:
  ```json
  [
    {
      "metric_id": "subscriber_count",
      "table": "telecom_daily",
      "value_column": "active_subscribers",
      "time_column": "report_date",
      "segment_columns": ["region", "plan_type"],
      "data_type": "numeric"
    }
  ]
  ```
- Agent uses schema for query routing and visualization suggestions.

### 6.2 Chat Interfaces
- **Global:** Parse NLQ → Select metrics → Invoke tools (query/forecast) → Render response (text + viz).
- **Component:** Pre-load metric context → Single-turn reasoning → Inline replies.

### 6.3 Visualizations
- Dynamic chart library (e.g., Chart.js/NgCharts): Line/bar/area/tables.
- Forecast overlays: Dashed lines with error bands.
- Interactivity: Zoom, hover tooltips, filter linkages.

### 6.4 APIs & Integrations
- All endpoints return JSON with error handling.
- LangGraph tools: DB query, forecast calc, schema reader.
- Logging for query traces.

### 6.5 Data Ingestion UI
- Drag-and-drop uploads.
- Real-time schema preview (e.g., column types inferred).
- Form-based connector setup with validation rules.

---

## 7. Non-Functional Requirements

| Category     | Requirement |
|--------------|-------------|
| **Performance** | API responses <2s (mock data); dashboard load <3s. |
| **Scalability** | Handle 100+ concurrent users; modular for DB sharding. |
| **Usability** | Intuitive chat (e.g., suggested prompts); accessible (WCAG 2.1 AA). |
| **Reliability** | 99% uptime; graceful degradation on errors (e.g., fallback metrics). |
| **Security** | Input sanitization; API auth stubs (OAuth/JWT future). |
| **Maintainability** | Clean code (PEP8, Angular style guide); 80% test coverage. |

---

## 8. System Architecture Overview

```
Angular 19 Frontend
├── Dashboard Layout (Dynamic Cards & Filters)
├── Global Chat Interface
├── Component Chat Widgets (Per-Metric)
└── Ingestion Wizard UI
    ↓ (HTTP/REST)
FastAPI Backend
├── Metrics Discovery Service
├── Query & Filter Engine
├── Forecasting/Scenario Engine
└── Chat Orchestration Layer
    ↓ (Tool Calls)
LangGraph Pipelines
├── Global Agent (Multi-Tool Chain)
└── Component Agent (Focused Chain)
    ↓ (SQL/ORM)
Data Layer
└── Mock DB (SQLite/CSV) → Real Connectors (Future)
```

- **Data Flow:** User input → API → Agent pipeline → DB query → Response rendering.
- **Deployment:** Dockerized services; Kubernetes-ready.

---

## 9. Success Metrics (KPIs)

### Product KPIs
- User adoption: 70% of sessions include chat interactions.
- Time savings: <5 min per insight query (vs. 30+ min manual).
- Satisfaction: NPS >8 from beta users.

### Technical KPIs
- Query success rate: >90% (agent accuracy).
- Forecast MAE: <5% on mock benchmarks.
- End-to-end latency: <2s average.

**Measurement:** Integrated analytics (e.g., Google Analytics, backend logs).

---

## 10. Risks & Mitigations

| Risk                          | Impact | Likelihood | Mitigation Strategy |
|-------------------------------|--------|------------|---------------------|
| Inconsistent DB schemas       | Medium | Medium    | Fallback rules for metric inference; schema validation UI. |
| Agent hallucination/misrouting | High   | Medium    | Context injection, guardrail prompts, human-in-loop for MVP. |
| Integration delays for real DBs | Low    | Low       | Modular adapters; prioritize mock validation. |
| UI complexity overwhelms users | Medium | High      | Iterative UX testing; progressive disclosure. |

---

## 11. Deliverables

- **Frontend:** Angular app with dashboard, chats, and ingestion UI.
- **Backend:** FastAPI server with APIs and engines.
- **AI:** Two LangGraph pipelines with sample prompts/tools.
- **Data:** Mock TMT datasets and auto-schema scripts.
- **Documentation:** API specs (Swagger), setup guide, architecture diagrams.
- **Testing:** Unit/integration tests; demo scripts for use cases.

**Timeline Estimate:** 8-10 weeks for MVP (2 weeks design, 4 weeks dev, 2 weeks test).

---

## 12. MVP Definition

The MVP delivers a functional end-to-end prototype:
- Auto-discovered metrics from mock TMT data.
- Interactive dashboard with 3-5 sample visualizations.
- Working global and component chats handling 80% of core queries.
- Basic forecasting/scenarios with linear models.
- UI for ingestion (no backend).
- Deployable stack (local/dev environment).

**Success Criteria:** End-to-end demo of use cases with <10% error rate.

---

## Final Summary Statement

This BRD outlines a robust, AI-centric TMT analytics platform that automates metric discovery, delivers conversational insights via dual chat modes, and enables predictive scenario planning—all built on Angular, FastAPI, LangGraph, and extensible data layers. It positions your organization for agile, data-driven decisions in dynamic TMT landscapes, with a clear path from MVP to enterprise scale. For questions or iterations, contact the Product Owner.
