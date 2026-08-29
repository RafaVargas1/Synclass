import { ScrollViewStyleReset } from 'expo-router/html';
import type { PropsWithChildren } from 'react';

/**
 * Documento HTML raiz do build web (expo-router). Único lugar de onde as
 * fontes do design-system (Spline Sans no corpo, Bebas Neue nos títulos de
 * destaque — issue de redesign art deco) são carregadas: sem esse link, as
 * variáveis `--font-display`/`--font-deco` de global.css caem no fallback
 * de sistema mesmo declarando o nome da fonte certa.
 *
 * Também o único lugar que carrega o "Sign in with Apple JS" (issue #212):
 * como este arquivo só existe no build web (não roda no nativo), não precisa
 * da guarda `Platform.OS === 'web'` que `lib/auth/apple.ts` usa — sem esse
 * `<script>`, `window.AppleID` nunca existe e `obterIdTokenApple()` sempre
 * devolve `null` silenciosamente (achado do dev-review desta issue). `async`
 * porque o SDK não é bloqueante pro primeiro paint; `obterIdTokenApple()` já
 * trata o caso do script ainda não ter carregado quando o usuário toca no
 * botão antes do `load`.
 */
export default function Root({ children }: PropsWithChildren) {
  return (
    <html lang="pt-BR">
      <head>
        <meta charSet="utf-8" />
        <meta httpEquiv="X-UA-Compatible" content="IE=edge" />
        <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no" />
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="" />
        <link
          rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Spline+Sans:wght@400;500;600;700&family=Bebas+Neue&display=swap"
        />
        <script
          src="https://appleid.cdn-apple.com/appleauth/static/jsapi/appleid/1/en_US/appleid.auth.js"
          async
        />
        <ScrollViewStyleReset />
      </head>
      <body>{children}</body>
    </html>
  );
}
