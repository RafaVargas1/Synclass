import { useNavigation } from 'expo-router';
import Head from 'expo-router/head';
import { useLayoutEffect } from 'react';

export type TituloDaAbaProps = { titulo: string };

/**
 * Define o título da aba do navegador (issue #81, causa raiz corrigida na
 * #133 — ver implementation.md da issue: expo-router sempre monta
 * Head.Provider/react-helmet-async por baixo, e só <Head><title>
 * sobrescreve o título de forma confiável; document.title imperativo
 * puro é pisado pelo Helmet). Também chama `setOptions` — o
 * `NavigationContainer` do próprio `expo-router` embute um
 * `useDocumentTitle` incondicional que usa `options.title` como fonte
 * preferida; sem isso ele cairia no fallback `route.name`. Cada tela deve
 * montar só UM `TituloDaAba` (direto ou via `Topbar`/`TopbarAutenticada`)
 * — dois na mesma árvore correm um contra o outro pelo `document.title`
 * final (achado da issue #133: `HomeTemplate` já tinha seu próprio
 * `Topbar`, um segundo `TituloDaAba` solto na tela colidia com o de
 * dentro dele). No native, `<Head>` é no-op — seguro renderizar
 * incondicionalmente, sem checar `Platform.OS`.
 */
export function TituloDaAba({ titulo }: TituloDaAbaProps) {
  const tituloCompleto = titulo.startsWith('Synclass') ? titulo : `Synclass - ${titulo}`;
  const navigation = useNavigation();

  useLayoutEffect(() => {
    navigation.setOptions({ title: tituloCompleto });
  }, [navigation, tituloCompleto]);

  return (
    <Head>
      <title>{tituloCompleto}</title>
    </Head>
  );
}
