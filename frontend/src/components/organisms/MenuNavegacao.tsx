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
 * `todasSecoes` inclui `SecaoMeuPerfil` (issue #129): o item fixo de perfil
 * precisa ser destacado quando a rota ativa é `/perfil` — antes só buscava
 * em `secoesDoPapel`, que nunca contém esse item.
 */
function encontrarSecaoAtiva(todasSecoes: Secao[], pathname: string): Secao | undefined {
  return todasSecoes.find((secao) => pathname === secao.href);
}

/** "Meu perfil" (#77 follow-up): item fixo do menu, igual em qualquer papel
 *  — antes vivia como um botão solto no corpo do Painel, sem seguir a
 *  convenção de ficar junto das demais opções de navegação (achado de UX
 *  reportado pelo usuário: um CTA do tamanho de uma ação primária para uma
 *  ação que é, na verdade, secundária/infrequente). */
const SecaoMeuPerfil: Secao = { label: 'Meu perfil', href: '/perfil' };

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
  const todasSecoes = [...secoes, SecaoMeuPerfil];
  const secaoAtiva = encontrarSecaoAtiva(todasSecoes, pathname);

  const exibirSeccoes = telaLarga || aberto;
  const temVariosPapeis = papeis.length > 1;

  return (
    <View className={telaLarga ? 'w-full' : 'relative'}>
      {telaLarga ? null : (
        <BotaoAlternarMenu aberto={aberto} aoAlternar={() => setAberto((atual) => !atual)} />
      )}
      {exibirSeccoes ? (
        <View
          className={
            telaLarga
              ? 'w-full flex-row flex-wrap items-center gap-three border-t border-border py-two dark:border-dark-border'
              : 'absolute left-0 top-full z-10 min-w-[220px] gap-two rounded-medium border border-border bg-background p-three shadow-md dark:border-dark-border dark:bg-dark-background'
          }
        >
          {temVariosPapeis ? (
            <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={onSelecionarPapel} />
          ) : null}
          <View className={telaLarga ? 'flex-row flex-wrap items-center gap-three' : 'gap-one'}>
            {todasSecoes.map((secao) => (
              <ItemDeSecao
                key={secao.label}
                secao={secao}
                ativo={secao.label === secaoAtiva?.label}
              />
            ))}
          </View>
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
      <IconeHamburguer aberto={aberto} />
    </Pressable>
  );
}

/** Ícone de menu sanduíche (☰) — três barras que viram um X ao abrir,
 *  seguindo a convenção de plataforma (reconhecimento > recordação, ver
 *  `docs/spec/ux-heuristics.md`). Antes era o texto literal "Abrir menu",
 *  que não é reconhecível como controle de menu à primeira vista. */
function IconeHamburguer({ aberto }: { aberto: boolean }) {
  const corDaBarra = 'bg-text dark:bg-dark-text';
  const barra2 = aberto
    ? { transform: [{ rotate: '45deg' }, { translateY: 6 }] }
    : undefined;
  const barra3 = aberto ? { opacity: 0 } : undefined;
  const barra4 = aberto
    ? { transform: [{ rotate: '-45deg' }, { translateY: -6 }] }
    : undefined;
  return (
    <View className="gap-one" style={{ width: 20 }}>
      <View className={`h-[2px] w-full ${corDaBarra}`} style={barra2} />
      <View className={`h-[2px] w-full ${corDaBarra}`} style={barra3} />
      <View className={`h-[2px] w-full ${corDaBarra}`} style={barra4} />
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
