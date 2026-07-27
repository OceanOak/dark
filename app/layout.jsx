import "./globals.css";

export const metadata = {
  title: "Darklang — Guest workspace",
  description:
    "A small dashboard prototype for trying Darklang as an anonymous user.",
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
