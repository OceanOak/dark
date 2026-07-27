import "./globals.css";

export const metadata = {
  title: "Darklang — Build software without assembling a stack",
  description:
    "An open-source, typed language and runtime with packages, source control, tracing, deployment, and sync built in.",
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
