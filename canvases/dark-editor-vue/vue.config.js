// vue.config.js

const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  // ...
  configureWebpack: {
    plugins: [
      new HtmlWebpackPlugin({
        filename: 'hashes.html',
        inject: false,
        templateContent: (templateParams, compilation) => {
          const hashes = {};
          Object.keys(compilation.assets).forEach((filename) => {
            if (filename.endsWith('.js') || filename.endsWith('.css')) {
              hashes[filename] = compilation.assets[filename].hash;
            }
          });
          return `
            <script>
              window.__hashes__ = ${JSON.stringify(hashes, null, 2)};
            </script>
          `;
        },
      }),
    ],
  },
};
