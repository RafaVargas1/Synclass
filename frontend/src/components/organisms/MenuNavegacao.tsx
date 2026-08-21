import { Link, usePathname } from 'expo-router';
import { Text, View } from 'react-native';

import type { AlternadorDePapelProps } from '@/components/organisms/AlternadorDePapel';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { secoesDoPapel, type Secao } from '@/lib/secoesPorPapel';
import { usePerfilLogado } from '@/lib/usePerfilLogado';

/**
 * Devolve a primeira seção cuja rota (já com o segmento dinâmico resolvido
 * para o `usuarioId` do logado, ex: `/professor/abc-123/horarios`) equivale
 * à rota ativa vinda de `usePathname`. Compara por igualdade de caminho:
 * as seções de Professor já embutem o `usuarioId` real do logado, então
 * cada rota do próprio Professor casa exatamente com a seção correspondente.
 */
function encontrarSecaoAtiva(secoes: Secao[], pathname: string): Secao | undefined {
  return secoes.find((secao) => pathname === secao.href);
}

/**
 * Organismo: menu de navegação persistente (#77). Resolve `token` via
 * `useSessao()` e `usuarioId` via `usePerfilLogado(token)` internamente
 * (mesma assinatura de props de `AlternadorDePapel` — decisão do task.md),
 * deriva a lista de seções via `secoesDoPapel` e destaca a seção ativa com
 * base em `usePathname` (match por segmento dinâmico).
 */
export function MenuNavegacao({ papeis, papelAtivo, onSelecionarPapel }: AlternadorDePapelProps) {
  const { token } = useSessao();
  const { usuarioId } = usePerfilLogado(token);
  const pathname = usePathname();
  const secoes = secoesDoPapel(papelAtivo, usuarioId);
  const secaoAtiva = encontrarSecaoAtiva(secoes, pathname);

  void papeis;
  void onSelecionarPapel;

  return (
    <View>
      {secoes.map((secao) => (
        <ItemDeSecao
          key={secao.label}
          secao={secao}
          ativo={secao.label === secaoAtiva?.label}
        />
      ))}
    </View>
  );
}

function ItemDeSecao({ secao, ativo }: { secao: Secao; ativo: boolean }) {
  const destaque = ativo ? 'bg-background-selected dark:bg-dark-background-selected' : '';
  return (
    <Link
      href={secao.href}
      accessibilityRole="link"
      accessibilityState={{ selected: ativo }}
      className={`border-b border-border px-four py-three dark:border-dark-border ${destaque}`}
    >
      <Text>{secao.label}</Text>
    </Link>
  );
}
