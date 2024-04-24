// @ts-check
// Note: type annotations allow type checking and IDEs autocompletion

require('dotenv').config()

const customFields = require('./src/customFields')
const prismThemes = require('prism-react-renderer').themes

/** @type {import('@docusaurus/types').Config} */
module.exports = {
  title: 'AR Magic Lantern SDK',
  tagline: '',
  url: process.env.DOCUSAURUS_URL,
  baseUrl: process.env.DOCUSAURUS_BASE_URL,
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'favicon.ico',
  customFields,

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'fubilab', // Usually your GitHub org/user name.
  projectName: 'arml-sdk', // Usually your repo name.

  // Even if you don't use internalization, you can use this field to set useful
  // metadata like html lang. For example, if your site is Chinese, you may want
  // to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          // sidebarPath: 'sidebars.js',
          routeBasePath: '/', // Serve the docs at the site's root
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          // editUrl:
          //   'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
        },
        // blog: {
        //   showReadingTime: true,
        //   // Please change this to your repo.
        //   // Remove this to remove the "edit this page" links.
        //   editUrl:
        //     'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
        // },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      colorMode: {
        defaultMode: 'dark',
        disableSwitch: true,
      },
      navbar: {
        title: 'AR Magic Lantern SDK',
        items: [
          {
            href: 'https://github.com/fubilab/arml-sdk',
            position: 'right',
            className: 'header-github-link',
            label: 'GitHub',
            'aria-label': 'GitHub repository',
          },
        ],
        logo: {
          alt: 'AR Magic Lantern logo',
          src: 'ARML-logo-circular.png',
          // srcDark: 'img/logo_dark.svg',
          // href: 'https://docusaurus.io/',
          // target: '_self',
          // width: 250,
          // height: 173,
          className: 'custom-navbar-logo-class',
          // style: {border: 'solid red'},
        },
      },
      footer: {
        style: 'dark',
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
      },
    }),
  plugins: [
    // [
    //   '@docusaurus/plugin-client-redirects',
    //   {
    //     redirects: [
    //       // /docs/oldDoc -> /docs/newDoc
    //       {
    //         from: '/docs',
    //         to: '/docs/guide/',
    //       },
    //     ],
    //   },
    // ],
    async function docusaurusTailwindCss() {
      return {
        name: 'docusaurus-tailwindcss',
        configurePostCss(postcssOptions) {
          // Appends TailwindCSS and AutoPrefixer.
          postcssOptions.plugins.push(require('tailwindcss'))
          postcssOptions.plugins.push(require('autoprefixer'))
          return postcssOptions
        },
        configureWebpack(config) {
          return {
            resolve: {
              ...(config.resolve ?? {}),
              fallback: {
                fs: false,
                tls: false,
                net: false,
                path: false,
                zlib: false,
                http: false,
                https: false,
                stream: false,
                crypto: false,
                os: false,
                vm: false,
                util: false,
                url: false,
              },
            },
          }
        },
      }
    },
  ],
}
