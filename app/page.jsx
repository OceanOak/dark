"use client";

import { useEffect, useState } from "react";

const installCommand = "curl -fsSL https://darklang.com/install | sh";

const featureSteps = [
  {
    id: "write",
    number: "01",
    eyebrow: "Write / Generate",
    title: "Start with code—or a prompt.",
    copy:
      "Functional, composable, and gradually typed: prototype fast, then tighten the types when it matters. Packages are available the moment you reference them—no installation step, no lockfile.",
    tags: [
      "Functional & composable",
      "Gradual typing",
      "Zero-install packages",
      "LSP & agent tooling",
    ],
    accent: "violet",
    visual: "editor",
  },
  {
    id: "run",
    number: "02",
    eyebrow: "Run",
    title: "Run it the moment it exists.",
    copy:
      "No build step, no environment to prepare. The gap between written and running is a single keystroke. The runtime is async by default—and it’s the same runtime in the CLI, the browser, and the cloud.",
    tags: ["No build step", "Async without keywords", "One runtime everywhere"],
    accent: "blue",
    visual: "terminal",
  },
  {
    id: "inspect",
    number: "03",
    eyebrow: "Inspect",
    title: "See exactly what happened.",
    copy:
      "Every execution leaves a trace—its inputs, outputs, and intermediate values, shown beside the expressions that produced them. Debug with real data instead of print statements, and replay the exact execution.",
    tags: ["Real inputs & outputs", "Values beside expressions", "Replayable executions"],
    accent: "mint",
    visual: "trace",
  },
  {
    id: "version",
    number: "04",
    eyebrow: "Version & Share",
    title: "Version it as you build.",
    copy:
      "Code lives in a content-addressed package tree, where functions, types, and values are versioned directly—not as mutable files. Branch, commit, and share the program where it lives. No separate repository, no Git plumbing.",
    tags: [
      "Content-addressed code",
      "Definition-level diffs",
      "Built-in branches & commits",
      "Share in one command",
    ],
    accent: "sand",
    visual: "version",
  },
  {
    id: "deploy",
    number: "05",
    eyebrow: "Deploy",
    title: "Make the same program live, anywhere.",
    copy:
      "HTTP handlers, typed datastores, crons, and background workers live in the program—not in YAML around it. Run the same version locally, on your own infrastructure, or on Darklang Cloud. Deployment makes a version live; there’s no artifact to build or ship.",
    tags: [
      "HTTP · Datastores · Crons · Workers",
      "Local · Self-hosted · Darklang Cloud",
      "Live in ~50 ms",
    ],
    accent: "rose",
    visual: "deploy",
  },
  {
    id: "sync",
    number: "06",
    eyebrow: "Sync",
    title: "Change it while it runs.",
    copy:
      "Because code is content-addressed and versioned, instances can exchange changes safely. Sync from your machine to a running deployment, make a new version live without downtime, or roll back by selecting an earlier one. Work offline and sync when you reconnect.",
    tags: [
      "Local ⇄ cloud sync",
      "Zero-downtime updates",
      "Rollback by version",
      "Offline-first development",
    ],
    accent: "lavender",
    visual: "sync",
  },
];

const buildCards = [
  {
    icon: "$",
    title: "CLI tools",
    copy: "Replace fragile Bash and Python scripts with typed, cross-platform tools that don’t rot.",
    link: "Explore the CLI",
  },
  {
    icon: "://",
    title: "Backends & APIs",
    copy: "Build endpoints, typed data, scheduled jobs, and workers as one complete program.",
    link: "Explore backends",
  },
  {
    icon: "⚡",
    title: "Automations",
    copy: "Connect webhooks, APIs, and recurring jobs with retries and tracing built in. Get paged only when intervention matters.",
    link: "Explore automations",
  },
  {
    icon: "✳",
    title: "AI apps & MCP servers",
    copy: "Build agents, tools, and MCP servers with tracing built in, so you can inspect every step and see exactly what ran.",
    link: "Explore AI apps",
  },
];

function Logo() {
  return (
    <a className="logo" href="#top" aria-label="Darklang home">
      <span className="logo-mark" aria-hidden="true">
        d
      </span>
      <span>darklang</span>
    </a>
  );
}

function Arrow() {
  return <span aria-hidden="true">↗</span>;
}

function ProductVisual({ type }) {
  if (type === "editor") {
    return (
      <div className="product-window editor-window">
        <WindowBar label="editor · Acme" />
        <div className="code-body">
          <CodeLine n="1">
            <span className="kw">let</span> greet <span className="dim">(</span>name
            <span className="dim">: </span><span className="type">String</span>
            <span className="dim">) : </span><span className="type">String</span>{" "}
            <span className="dim">=</span>
          </CodeLine>
          <CodeLine n="2">
            <span className="str">&quot;Hello, &quot;</span>{" "}
            <span className="op">++</span> name
          </CodeLine>
          <CodeLine n="3">&nbsp;</CodeLine>
          <CodeLine n="4">
            <span className="kw">let</span> shout{" "}
            <span className="dim">(</span>name<span className="dim">: </span>
            <span className="type">String</span><span className="dim">) : </span>
            <span className="type">String</span> <span className="dim">=</span>
          </CodeLine>
          <CodeLine n="5">
            greet name <span className="op">|&gt;</span>{" "}
            <span className="namespace">Stdlib.String.</span>
            <span className="cursor" />
          </CodeLine>
          <div className="autocomplete">
            <div className="autocomplete-note">packages just work · no install</div>
            <div className="completion selected">
              <span>toUppercase</span><small>String → String</small>
            </div>
            <div className="completion">
              <span>trim</span><small>String → String</small>
            </div>
            <div className="completion">
              <span>length</span><small>String → Int</small>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (type === "terminal") {
    return (
      <div className="product-window">
        <WindowBar label="terminal" />
        <div className="terminal-body">
          <p><span className="prompt">$</span> dark eval <span className="str">&apos;shout &quot;world&quot;&apos;</span></p>
          <p className="terminal-result">&quot;HELLO, WORLD&quot; <em>ran instantly · no build</em></p>
          <p className="terminal-gap"><span className="prompt">$</span> dark eval <span className="str">&apos;[1; 2] |&gt; Stdlib.List.map shout&apos;</span></p>
          <p className="terminal-error">error: shout expects a String, got Int</p>
          <p className="terminal-code">[1; 2] |&gt; Stdlib.List.map shout</p>
          <p className="terminal-caret">                              ^^^^^</p>
        </div>
      </div>
    );
  }

  if (type === "trace") {
    return (
      <div className="product-window trace-window">
        <WindowBar label="trace #1042 · shout “world”" live />
        <div className="trace-body">
          <div className="trace-summary">
            <div>
              <span>Execution</span>
              <strong>shout “world”</strong>
            </div>
            <span className="trace-duration">2 ms</span>
          </div>
          <div className="trace-path">
            <div className="trace-step">
              <span className="trace-index">input</span>
              <div><code>name</code><strong>“world”</strong></div>
              <span className="trace-check">✓</span>
            </div>
            <div className="trace-step">
              <span className="trace-index">01</span>
              <div><code>greet name</code><strong>“Hello, world”</strong></div>
              <span className="trace-check">✓</span>
            </div>
            <div className="trace-step">
              <span className="trace-index">02</span>
              <div><code>toUppercase</code><strong>“HELLO, WORLD”</strong></div>
              <span className="trace-check">✓</span>
            </div>
          </div>
          <div className="trace-footer">
            <span><i /> saved with real inputs</span>
            <button type="button">Replay execution ↗</button>
          </div>
        </div>
      </div>
    );
  }

  if (type === "version") {
    return (
      <div className="product-window">
        <WindowBar label="changes · branch: greet-shout" />
        <div className="terminal-body status-body">
          <p><span className="prompt">$</span> dark status</p>
          <div className="change-line">
            <span className="change-mod">~</span>
            <code>Acme.greet</code>
            <span>v1 → v2</span>
            <small>3 dependents · compatible</small>
          </div>
          <div className="change-line">
            <span className="change-add">+</span>
            <code>Acme.shout</code>
            <span>new fn</span>
            <small>content addressed</small>
          </div>
          <p className="terminal-gap"><span className="prompt">$</span> dark share Acme.shout</p>
          <p className="terminal-result">→ callable by anyone, right now</p>
        </div>
      </div>
    );
  }

  if (type === "deploy") {
    return (
      <div className="product-window">
        <WindowBar label="deploy" />
        <div className="terminal-body deploy-body">
          <p><span className="prompt">$</span> dark deploy Acme.shout <span className="dim">--target cloud</span></p>
          <div className="deploy-grid">
            <span>target</span><strong>Darklang Cloud · eu-west</strong>
            <span>creates</span><strong>POST /shout</strong>
            <span>version</span><strong>sha256:8c3…a91</strong>
            <span>status</span><strong className="deploy-live"><i /> live in 50 ms</strong>
          </div>
          <div className="artifact-note">no containers · no pipelines · no artifact</div>
        </div>
      </div>
    );
  }

  return (
    <div className="product-window sync-window">
      <WindowBar label="sync · instances" live />
      <div className="sync-body">
        <div className="sync-nodes">
          <div className="sync-node">
            <span className="node-icon">⌁</span>
            <div><small>source</small><strong>Laptop</strong><span>Acme.shout v2</span></div>
          </div>
          <div className="sync-flow">
            <i /><strong>⇄</strong><i />
            <small>content verified</small>
          </div>
          <div className="sync-node">
            <span className="node-icon">◇</span>
            <div><small>running</small><strong>Cloud</strong><span>Acme.shout v2</span></div>
          </div>
        </div>
        <div className="sync-events">
          <p><span className="event-icon">↓</span><code>Stdlib.Http v14</code><small>pulled · pure upgrade</small><b>done</b></p>
          <p><span className="event-icon">↑</span><code>Acme.shout v2</code><small>pushed · hash matched</small><b>done</b></p>
          <p><span className="event-icon live">●</span><code>Running app updated</code><small>traffic never stopped</small><b>live</b></p>
        </div>
        <div className="rollback">
          <code><span className="prompt">$</span> dark rollback Acme.shout v1</code>
          <span>choose any previous version <strong>↗</strong></span>
        </div>
      </div>
    </div>
  );
}

function WindowBar({ label, live = false }) {
  return (
    <div className="window-bar">
      <div className="window-dots" aria-hidden="true"><i /><i /><i /></div>
      <span>{label}</span>
      {live && <span className="live-indicator"><i /> live</span>}
    </div>
  );
}

function CodeLine({ n, children }) {
  return (
    <div className="code-line">
      <span className="line-number">{n}</span>
      <code>{children}</code>
    </div>
  );
}

export default function Home() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    const nodes = document.querySelectorAll("[data-reveal]");
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) entry.target.classList.add("is-visible");
        });
      },
      { threshold: 0.12 }
    );
    nodes.forEach((node) => observer.observe(node));
    return () => observer.disconnect();
  }, []);

  const copyInstall = async () => {
    await navigator.clipboard.writeText(installCommand);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1600);
  };

  return (
    <main id="top">
      <nav className="site-nav" aria-label="Primary navigation">
        <div className="nav-inner">
          <Logo />
          <button
            className="menu-button"
            onClick={() => setMenuOpen((open) => !open)}
            aria-expanded={menuOpen}
            aria-controls="nav-links"
          >
            <span /><span />
            <span className="sr-only">Toggle navigation</span>
          </button>
          <div className={`nav-links ${menuOpen ? "is-open" : ""}`} id="nav-links">
            <a href="#how" onClick={() => setMenuOpen(false)}>Product</a>
            <a href="#ai" onClick={() => setMenuOpen(false)}>AI</a>
            <a href="#architecture" onClick={() => setMenuOpen(false)}>Packages</a>
            <a href="https://docs.darklang.com">Docs</a>
            <a href="https://darklang.com/discord-invite">Community</a>
            <a href="https://github.com/darklang/dark">GitHub</a>
          </div>
          <a className="nav-cta" href="#start">Try Darklang <span>↗</span></a>
        </div>
      </nav>

      <section className="hero">
        <div className="hero-grid" aria-hidden="true" />
        <div className="glow glow-one" aria-hidden="true" />
        <div className="glow glow-two" aria-hidden="true" />
        <div className="page-shell hero-inner">
          <div className="hero-copy" data-reveal>
            <div className="hero-kicker">
              <span className="pulse-dot" /> An open-source programming language & runtime
            </div>
            <h1>Build software<br /><span>without assembling a stack.</span></h1>
            <p className="hero-lede">
              Darklang brings the language, runtime, package management, source control,
              and code review into one integrated system. Write code and run it instantly,
              use the AI coding agent you prefer, and deploy the same program locally, on
              your own infrastructure, or to Darklang Cloud.
            </p>
            <div className="hero-actions">
              <a className="button button-primary" href="#start">Try it in your browser <Arrow /></a>
              <a className="text-link" href="#how">See how it works <span>↓</span></a>
            </div>
            <div className="trust-row">
              <span><i /> Apache 2.0</span>
              <span>Runs locally</span>
              <span>Self-hostable</span>
            </div>
          </div>

          <div className="hero-product" data-reveal>
            <div className="hero-orbit orbit-one" aria-hidden="true" />
            <div className="hero-orbit orbit-two" aria-hidden="true" />
            <div className="hero-terminal">
              <WindowBar label="dark · Acme" live />
              <div className="hero-terminal-tabs">
                <span className="active">program</span><span>trace</span><span>versions</span>
              </div>
              <div className="hero-code">
                <CodeLine n="1"><span className="kw">let</span> greet <span className="dim">(</span>name<span className="dim">: </span><span className="type">String</span><span className="dim">) : </span><span className="type">String</span> <span className="dim">=</span></CodeLine>
                <CodeLine n="2"><span className="str">&quot;Hello, &quot;</span> <span className="op">++</span> name</CodeLine>
                <CodeLine n="3">&nbsp;</CodeLine>
                <CodeLine n="4"><span className="kw">let</span> shout <span className="dim">(</span>name<span className="dim">: </span><span className="type">String</span><span className="dim">) : </span><span className="type">String</span> <span className="dim">=</span></CodeLine>
                <CodeLine n="5">greet name <span className="op">|&gt;</span> <span className="namespace">Stdlib.String.toUppercase</span></CodeLine>
              </div>
              <div className="hero-run">
                <span className="run-icon">▶</span>
                <div><small>latest execution</small><strong>“HELLO, WORLD”</strong></div>
                <span className="run-time">2 ms</span>
              </div>
              <div className="hero-status">
                <span><i /> running locally</span><span>version 8c3…a91</span>
              </div>
            </div>
            <div className="floating-chip chip-package">package resolved <strong>0 ms</strong></div>
            <div className="floating-chip chip-trace">trace saved <strong>#1042</strong></div>
          </div>

          <button className="install-command" onClick={copyInstall} title="Copy install command">
            <span className="prompt">$</span>
            <code>{installCommand}</code>
            <span className="copy-state">{copied ? "copied!" : "copy"}</span>
          </button>
        </div>
      </section>

      <section className="workflow section" id="how">
        <div className="page-shell">
          <div className="section-heading workflow-heading" data-reveal>
            <span className="section-kicker">How it works</span>
            <h2>One program, from first line to production.</h2>
            <p>
              Shipping even a small feature can mean assembling a runtime, package manager,
              framework, container, CI pipeline, and hosting—all before you build the thing
              you actually care about. Follow a single program through one integrated workflow.
            </p>
          </div>

          <div className="workflow-list">
            {featureSteps.map((step, index) => (
              <article
                className={`workflow-step ${index % 2 ? "reverse" : ""}`}
                id={step.id}
                key={step.id}
                data-reveal
              >
                <div className="step-copy">
                  <div className="step-meta">
                    <span className="step-number">{step.number}</span>
                    <span className={`step-eyebrow ${step.accent}`}>{step.eyebrow}</span>
                  </div>
                  <h3>{step.title}</h3>
                  <p>{step.copy}</p>
                  <div className="tag-list">
                    {step.tags.map((tag) => <span key={tag}>{tag}</span>)}
                  </div>
                </div>
                <div className={`step-visual accent-${step.accent}`}>
                  <ProductVisual type={step.visual} />
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="architecture section" id="architecture">
        <div className="page-shell" data-reveal>
          <div className="section-heading architecture-heading">
            <span className="section-kicker">Why it works</span>
            <h2>The pieces were designed together.</h2>
            <p>
              Most stacks connect separate tools after the fact. Here, the language, package
              tree, runtime, and infrastructure share one representation of the program—so
              each part can understand the others directly.
            </p>
          </div>
          <div className="system-roles">
            <a href="#write">
              <span><i>01</i>{"{ }"}</span>
              <h3>Language</h3>
              <p>Defines the program.</p>
              <small>What you write</small>
            </a>
            <a href="#version">
              <span><i>02</i>◈</span>
              <h3>Package tree</h3>
              <p>Stores and versions it.</p>
              <small>What changes</small>
            </a>
            <a href="#run">
              <span><i>03</i>▶</span>
              <h3>Runtime</h3>
              <p>Runs it immediately.</p>
              <small>What executes</small>
            </a>
            <a href="#deploy">
              <span><i>04</i>⇄</span>
              <h3>Infrastructure</h3>
              <p>Makes it live.</p>
              <small>What goes live</small>
            </a>
          </div>
          <div className="system-note">
            <strong>One content-addressed program</strong>
            <span>No translation layer</span><i />
            <span>No artifact handoff</span><i />
            <span>No configuration drift</span>
          </div>
        </div>
      </section>

      <section className="ai-section section" id="ai">
        <div className="ai-grid" aria-hidden="true" />
        <div className="page-shell ai-layout">
          <div className="ai-copy" data-reveal>
            <span className="section-kicker light">Darklang for AI</span>
            <h2>Let AI write code.<br /><span>Make it show its work.</span></h2>
            <p>
              Agents don’t get a folder of text files. They get the same environment you have:
              packages, execution, traces, and types. Bring the agent you prefer; several can
              even work the same request on separate branches.
            </p>
            <p>
              Before you accept a change, see affected callers, failing type-checks, changed
              recorded outputs, and new access to data or external services. A person controls
              merge and deployment.
            </p>
            <div className="ai-tags">
              <span>Terminal agents</span><span>Editor assistants</span>
              <span>MCP-compatible</span><span>Local or hosted models</span>
            </div>
          </div>
          <div className="review-panel" data-reveal>
            <WindowBar label="change review · User.get" />
            <div className="review-signature">
              <span>Signature</span>
              <code><b>~</b> User → Result&lt;User&gt;</code>
            </div>
            <div className="review-list">
              <div><strong>1,247</strong><span>callers affected</span><small className="ok">analyzed</small></div>
              <div><strong>3</strong><span>callers fail type-checking</span><small className="block">blocking</small></div>
              <div><strong>5</strong><span>recorded inputs changed output</span><small>evidence attached</small></div>
              <div><strong>1</strong><span>new external request</span><small className="warn">api.stripe.com</small></div>
            </div>
            <div className="review-actions">
              <button>Run the evidence</button><button className="accept">Accept change</button>
            </div>
          </div>
        </div>
      </section>

      <section className="build-section section" id="build">
        <div className="page-shell">
          <div className="section-heading" data-reveal>
            <span className="section-kicker">What you can build</span>
            <h2>Start small. Grow without starting over.</h2>
          </div>
          <div className="build-grid">
            {buildCards.map((card) => (
              <a className="build-card" href="#start" key={card.title} data-reveal>
                <span className="build-icon">{card.icon}</span>
                <h3>{card.title}</h3>
                <p>{card.copy}</p>
                <span className="card-link">{card.link} <Arrow /></span>
              </a>
            ))}
          </div>
          <div className="also-row" data-reveal>
            <span>Also useful for</span>
            <div>
              <a href="#start">Local-first tools</a><a href="#start">Security tooling</a>
              <a href="#start">Web scrapers</a><a href="#start">Internal tools</a>
              <a href="#start">Small-business software</a>
            </div>
          </div>
        </div>
      </section>

      <section className="open-source section">
        <div className="page-shell open-source-card" data-reveal>
          <div className="open-source-copy">
            <div>
              <span className="section-kicker">Open source</span>
              <h2>Run it your way.</h2>
              <p>
                The language, runtime, and tooling are Apache 2.0—the whole product, not a
                limited community tier. Develop locally, self-host on your own infrastructure,
                or use Darklang Cloud. Hosting is optional; your code is never tied to our cloud.
              </p>
            </div>
            <div className="license-proof">
              <span>{"{ }"}</span>
              <div><small>License</small><strong>Apache 2.0</strong><p>Language · runtime · tooling</p></div>
            </div>
          </div>
          <div className="hosting-options">
            <div><span>⌁</span><small>01</small><strong>Local</strong><p>The complete runtime on your machine.</p><b>Runs offline</b></div>
            <div><span>◇</span><small>02</small><strong>Self-hosted</strong><p>Your infrastructure and operating rules.</p><b>You control it</b></div>
            <div><span>✦</span><small>03</small><strong>Darklang Cloud</strong><p>Managed hosting, live in milliseconds.</p><b>Optional</b></div>
          </div>
          <div className="open-source-footer">
            <a className="button button-dark" href="https://github.com/darklang/dark">
              View source on GitHub <Arrow />
            </a>
            <div className="open-links">
              <a href="https://github.com/darklang/dark">Project status & roadmap <Arrow /></a>
              <a href="https://docs.darklang.com/contributing/repo-layout">Contribute <Arrow /></a>
              <a href="https://github.com/darklang/classic-dark">Darklang Classic <Arrow /></a>
            </div>
          </div>
        </div>
      </section>

      <section className="final-cta section" id="start">
        <div className="final-glow" aria-hidden="true" />
        <div className="page-shell" data-reveal>
          <span className="section-kicker light">Follow along</span>
          <h2>Watch it get built.</h2>
          <p>Releases, technical deep-dives, and project milestones. A few emails a month, no growth-hacking.</p>
          <form className="signup-form" onSubmit={(event) => event.preventDefault()}>
            <label className="sr-only" htmlFor="email">Email address</label>
            <input id="email" type="email" placeholder="you@example.com" />
            <button type="submit">Get project updates <Arrow /></button>
          </form>
          <div className="start-card">
            <div>
              <span>Ready when you are</span>
              <h3>Start building with Darklang.</h3>
            </div>
            <div>
              <a className="button button-primary" href="#top">Try it in your browser <Arrow /></a>
              <a className="button button-ghost" href="https://github.com/darklang/dark">View on GitHub <Arrow /></a>
            </div>
          </div>
        </div>
      </section>

      <footer>
        <div className="page-shell footer-grid">
          <div className="footer-brand">
            <Logo />
            <p>An open-source language, runtime, and package tree for the whole development loop.</p>
          </div>
          <div className="footer-column">
            <strong>Product</strong>
            <a href="#write">Language</a><a href="#run">Runtime</a>
            <a href="#architecture">Packages</a><a href="#deploy">Backends</a>
            <a href="#build">CLI</a><a href="#ai">AI</a>
          </div>
          <div className="footer-column">
            <strong>Resources</strong>
            <a href="#top">Getting started</a><a href="https://docs.darklang.com">Documentation</a>
            <a href="https://github.com/darklang/dark">Project status</a><a href="https://darklang.com/discord-invite">Support</a>
          </div>
          <div className="footer-column">
            <strong>Community</strong>
            <a href="https://github.com/darklang/dark">GitHub</a><a href="https://darklang.com/discord-invite">Discord</a>
            <a href="https://blog.darklang.com">Blog</a><a href="#start">Newsletter</a>
          </div>
        </div>
        <div className="page-shell footer-bottom">
          <span>© Darklang Inc. · Apache License 2.0</span>
          <div><a href="https://github.com/darklang/classic-dark">Darklang Classic</a><a href="https://darklang.com/privacy">Privacy</a><a href="https://darklang.com/terms">Terms</a></div>
        </div>
      </footer>
    </main>
  );
}
