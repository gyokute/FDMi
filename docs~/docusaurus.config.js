// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import { themes as prismThemes } from 'prism-react-renderer';

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'FDMi',
  tagline: 'Flight Dynamics Model improved.',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://gyokute.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/FDMi/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'gyokute', // Usually your GitHub org/user name.
  projectName: 'FDMi', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'ja',
    locales: ['ja'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.js',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/gyokute/FDMi/tree/main/docs~/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/docusaurus-social-card.jpg',
      navbar: {
        title: 'FDMi',
        logo: {
          alt: 'FDMi Logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Documents',
          },
          {
            href: 'https://github.com/gyokute/FDMi',
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Tutorial',
                to: '/intro',
              },
            ],
          },
          {
            title: 'VRChat Worlds',
            items: [
              {
                label: 'GenericSkyWorlds-TFFJ',
                href: 'https://vrchat.com/home/launch?worldId=wrld_cc87574e-feae-4fb0-aa97-7bdac8ff9f47 ',
              }
            ],
          },
          {
            title: 'More',
            items: [
              {
                label: 'GitHub - FDMi',
                href: 'https://github.com/gyokute/FDMi',
              },
              {
                label: 'Twitter',
                href: 'https://twitter.com/gyo_ku_te',
              }
            ],
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} gyokute. Built with Docusaurus.`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
      },
    }),
};

export default config;
