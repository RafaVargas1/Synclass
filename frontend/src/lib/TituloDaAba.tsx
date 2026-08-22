import Head from 'expo-router/head';

export type TituloDaAbaProps = { titulo: string };

/**
 * Define o título da aba do navegador (issue #81, causa raiz corrigida na
 * #133 — ver implementation.md da issue: expo-router sempre monta
 * Head.Provider/react-helmet-async por baixo, e só <Head><title>
 * sobrescreve o título de forma confiável; document.title imperativo é
 * pisado pelo Helmet). No native, <Head> é no-op — seguro renderizar
 * incondicionalmente, sem checar Platform.OS.
 */
export function TituloDaAba({ titulo }: TituloDaAbaProps) {
  const tituloCompleto = titulo.startsWith('Synclass') ? titulo : `Synclass - ${titulo}`;
  return (
    <Head>
      <title>{tituloCompleto}</title>
    </Head>
  );
}
