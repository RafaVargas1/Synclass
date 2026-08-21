import { Link, usePathname } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { AlternadorDePapel, type AlternadorDePapelProps } from '@/components/organisms/AlternadorDePapel';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { secoesDoPapel, type Secao } from '@/lib/secoesPorPapel';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { AlvoDeToqueMinimo } from '@/theme/tokens';

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
 * base em `usePathname` (match por segmento dinâmico). Quando há mais de um
 * papel, inclui `AlternadorDePapel` para trocar o papel ativo (issue #4) —
 * junto com as seções, não solto: em viewport estreita e menu fechado, nem
 * o alternador nem as seções aparecem (achado de dev-review, PR #107:
 * o alternador não pode flutuar independente do estado aberto/fechado do
 * próprio menu que o contém).
 *
 * Dois modos de exibição conforme `useIsTelaLarga`: em viewport larga as
 * seções ficam sempre visíveis; em estreita o menu começa fechado e só
 * aparece ao acionar o botão de abrir (e volta a esconder-se com o de
 * fechar).
 */
export function MenuNavegacao({ papeis, papelAtivo, onSelecionarPapel }: AlternadorDePapelProps) {
  const { token } = useSessao();
  const { usuarioId } = usePerfilLogado(token);
  const pathname = usePathname();
  const telaLarga = useIsTelaLarga();
  const [aberto, setAberto] = useState(false);
  const secoes = secoesDoPapel(papelAtivo, usuarioId);
  const secaoAtiva = encontrarSecaoAtiva(secoes, pathname);

  const exibirSeccoes = telaLarga || aberto;
  const temVariosPapeis = papeis.length > 1;

  return (
    <View>
      {telaLarga ? null : (
        <BotaoAlternarMenu aberto={aberto} aoAlternar={() => setAberto((atual) => !atual)} />
      )}
      {exibirSeccoes && temVariosPapeis ? (
        <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={onSelecionarPapel} />
      ) : null}
      {exibirSeccoes ? (
        <View>
          {secoes.map((secao) => (
            <ItemDeSecao
              key={secao.label}
              secao={secao}
              ativo={secao.label === secaoAtiva?.label}
            />
          ))}
        </View>
      ) : null}
    </View>
  );
}

function BotaoAlternarMenu({ aberto, aoAlternar }: { aberto: boolean; aoAlternar: () => void }) {
  const rotulo = aberto ? 'Fechar menu' : 'Abrir menu';
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={rotulo}
      onPress={aoAlternar}
      className="items-center justify-center"
      style={AlvoDeToqueMinimo}
    >
      <Text>{rotulo}</Text>
    </Pressable>
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
