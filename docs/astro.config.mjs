// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import markdownForAgents from '@puralex/astro-markdown-for-agents';

// https://astro.build/config
export default defineConfig({
	site: 'https://hertzole.se',
	base: '/sourcegen-utils/',
	integrations: [
		starlight({
			title: 'SourceGen Utils',
			customCss: [
				'./src/style.css'
			],
			social: [{ icon: 'github', label: 'GitHub', href: 'https://github.com/Hertzole/sourcegen-utils' }],
			sidebar: [
				{
					label: 'Guides',
					items: [
						// Each item here is one entry in the navigation menu.
						{ label: 'Getting Started', slug: 'getting-started' },
						{ label: 'Trimmer', slug: 'trimmer' },
						{ label: 'Assembly Attributes', slug: 'assembly-attributes' },
						{ label: 'Changelog', slug: 'changelog' },
					],
				},
				{
					label: 'API',
					items: [{ autogenerate: { directory: 'api' } }],
				},
			],
		}),
		markdownForAgents({
			siteTitle: 'SourceGen Utils',
			siteDescription: 'Installation, API, and operations documentation.',
		}),
	],
});
