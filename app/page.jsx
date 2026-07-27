"use client";

import { useMemo, useState } from "react";

const templates = [
  {
    icon: "↗",
    tone: "violet",
    title: "Hello, HTTP",
    description: "A tiny JSON API with one route.",
    kind: "API",
  },
  {
    icon: "⌁",
    tone: "green",
    title: "Webhook catcher",
    description: "Receive, inspect, and replay requests.",
    kind: "Automation",
  },
  {
    icon: "◴",
    tone: "amber",
    title: "Scheduled task",
    description: "Run a function on a daily schedule.",
    kind: "Automation",
  },
];

const activitySeed = [
  { event: "GET /hello", status: "200", duration: "18 ms", when: "just now" },
  { event: "GET /hello?name=Ada", status: "200", duration: "22 ms", when: "2m ago" },
];

function Brand() {
  return (
    <div className="brand" aria-label="Darklang">
      <span className="brand-mark">d</span>
      <span>darklang</span>
    </div>
  );
}

function Icon({ children }) {
  return <span className="nav-icon" aria-hidden="true">{children}</span>;
}

export default function Dashboard() {
  const [activeNav, setActiveNav] = useState("Home");
  const [filter, setFilter] = useState("All");
  const [noticeVisible, setNoticeVisible] = useState(true);
  const [activity, setActivity] = useState(activitySeed);
  const [isRunning, setIsRunning] = useState(false);
  const [copied, setCopied] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  const filteredTemplates = useMemo(
    () => templates.filter((template) => filter === "All" || template.kind === filter),
    [filter],
  );

  function runSample() {
    if (isRunning) return;
    setIsRunning(true);
    window.setTimeout(() => {
      setActivity((items) => [
        { event: "GET /hello", status: "200", duration: "16 ms", when: "just now" },
        ...items.map((item, index) => ({ ...item, when: index === 0 ? "1m ago" : item.when })),
      ].slice(0, 3));
      setIsRunning(false);
    }, 650);
  }

  async function copyUrl() {
    try {
      await navigator.clipboard.writeText("https://guest-7f3.darklang.app/hello");
    } catch {
      // Clipboard access can be unavailable in embedded previews.
    }
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1600);
  }

  function chooseNav(label) {
    setActiveNav(label);
    setMenuOpen(false);
  }

  return (
    <main className="dashboard">
      <aside className={`sidebar ${menuOpen ? "is-open" : ""}`}>
        <div className="sidebar-head">
          <Brand />
          <button className="close-menu" type="button" onClick={() => setMenuOpen(false)} aria-label="Close navigation">×</button>
        </div>

        <nav className="main-nav" aria-label="Main navigation">
          {[
            ["Home", "⌂"],
            ["Canvases", "◇"],
            ["Packages", "⌘"],
            ["Traces", "⌁"],
          ].map(([label, icon]) => (
            <button
              className={activeNav === label ? "active" : ""}
              type="button"
              key={label}
              onClick={() => chooseNav(label)}
            >
              <Icon>{icon}</Icon>
              {label}
              {label === "Canvases" && <span className="nav-count">1</span>}
            </button>
          ))}
        </nav>

        <div className="sidebar-section">
          <span className="sidebar-label">Guest workspace</span>
          <button className="workspace-row active" type="button">
            <span className="workspace-avatar">g</span>
            <span><strong>guest-7f3</strong><small>temporary</small></span>
            <span className="more">•••</span>
          </button>
        </div>

        <div className="sidebar-bottom">
          <div className="guest-card">
            <span className="guest-card-icon">✦</span>
            <strong>Keep your work</strong>
            <p>Sign in to save canvases and deploy to a permanent URL.</p>
            <button type="button">Create free account</button>
          </div>
          <button className="support-link" type="button"><Icon>?</Icon> Docs & support <span>↗</span></button>
        </div>
      </aside>

      {menuOpen && <button className="backdrop" aria-label="Close navigation" onClick={() => setMenuOpen(false)} />}

      <section className="workspace">
        <header className="topbar">
          <div className="mobile-brand">
            <button className="menu-button" type="button" onClick={() => setMenuOpen(true)} aria-label="Open navigation">☰</button>
            <Brand />
          </div>
          <div className="breadcrumbs">
            <span>guest-7f3</span><b>/</b><strong>{activeNav.toLowerCase()}</strong>
          </div>
          <div className="top-actions">
            <span className="connection"><i /> Runtime connected</span>
            <button className="signin-button" type="button">Sign in</button>
          </div>
        </header>

        <div className="content">
          {noticeVisible && (
            <div className="guest-notice" role="status">
              <span className="notice-icon">◷</span>
              <div>
                <strong>You’re building as a guest</strong>
                <p>This workspace expires in 24 hours. Sign in anytime to keep everything.</p>
              </div>
              <button className="notice-action" type="button">Sign in to save</button>
              <button className="dismiss" type="button" onClick={() => setNoticeVisible(false)} aria-label="Dismiss">×</button>
            </div>
          )}

          <section className="welcome">
            <div>
              <span className="eyebrow">YOUR WORKSPACE</span>
              <h1>Good afternoon.</h1>
              <p>Start with a working example, or make something from scratch.</p>
            </div>
            <button className="new-canvas" type="button"><span>+</span> New canvas</button>
          </section>

          <section className="starter-card">
            <div className="starter-copy">
              <span className="starter-badge"><i /> LIVE SAMPLE</span>
              <h2>Your first endpoint is already running.</h2>
              <p>Change the code, run it, and inspect what happened. Nothing to configure.</p>
              <div className="endpoint">
                <span className="method">GET</span>
                <code>guest-7f3.darklang.app/hello</code>
                <button type="button" onClick={copyUrl}>{copied ? "Copied!" : "Copy"}</button>
              </div>
              <div className="starter-actions">
                <button className="open-canvas" type="button">Open canvas <span>→</span></button>
                <button className="run-button" type="button" onClick={runSample}>
                  <span>{isRunning ? "…" : "▶"}</span> {isRunning ? "Running" : "Run sample"}
                </button>
              </div>
            </div>

            <div className="code-panel" aria-label="Darklang code sample">
              <div className="code-toolbar">
                <div><i /><i /><i /></div>
                <span>hello.dark</span>
                <span className="saved">● saved</span>
              </div>
              <div className="code">
                <div><span className="line">1</span><code><b>let</b> hello <em>name</em> =</code></div>
                <div><span className="line">2</span><code>  <strong>$&quot;Hello, &#123;name&#125;!&quot;</strong></code></div>
                <div><span className="line">3</span><code>&nbsp;</code></div>
                <div><span className="line">4</span><code><b>Http.get</b> <strong>&quot;/hello&quot;</strong> (<em>request</em> -&gt;</code></div>
                <div><span className="line">5</span><code>  <b>let</b> name = request.query</code></div>
                <div><span className="line">6</span><code>    |&gt; Dict.get <strong>&quot;name&quot;</strong></code></div>
                <div><span className="line">7</span><code>    |&gt; Option.default <strong>&quot;world&quot;</strong></code></div>
                <div><span className="line">8</span><code>  Http.response (hello name)</code></div>
                <div><span className="line">9</span><code>)</code></div>
              </div>
              <div className="code-result">
                <span>200 OK</span>
                <code>Hello, world!</code>
                <small>{isRunning ? "running…" : "16 ms"}</small>
              </div>
            </div>
          </section>

          <div className="lower-grid">
            <section className="templates-section">
              <div className="section-heading">
                <div><h2>Start from a template</h2><p>Small, useful things to make your own.</p></div>
                <div className="filters" aria-label="Filter templates">
                  {["All", "API", "Automation"].map((item) => (
                    <button className={filter === item ? "active" : ""} type="button" key={item} onClick={() => setFilter(item)}>{item}</button>
                  ))}
                </div>
              </div>

              <div className="template-list">
                {filteredTemplates.map((template) => (
                  <button className="template-card" type="button" key={template.title}>
                    <span className={`template-icon ${template.tone}`}>{template.icon}</span>
                    <span><strong>{template.title}</strong><small>{template.description}</small></span>
                    <span className="template-arrow">→</span>
                  </button>
                ))}
              </div>
            </section>

            <section className="activity-section">
              <div className="section-heading">
                <div><h2>Recent traces</h2><p>Every run, ready to inspect.</p></div>
                <button className="view-all" type="button">View all</button>
              </div>
              <div className="activity-card">
                {activity.map((item, index) => (
                  <button className="activity-row" type="button" key={`${item.when}-${index}`}>
                    <span className="trace-status">✓</span>
                    <span className="trace-name"><strong>{item.event}</strong><small>{item.when}</small></span>
                    <code>{item.status}</code>
                    <small>{item.duration}</small>
                    <span>›</span>
                  </button>
                ))}
                {activity.length === 0 && <p className="empty-state">Run the sample to create your first trace.</p>}
              </div>
            </section>
          </div>

          <footer className="dashboard-footer">
            <span>Darklang Cloud · Guest session</span>
            <span><i /> All systems operational</span>
          </footer>
        </div>
      </section>
    </main>
  );
}
