import { fireEvent, render, screen } from '@testing-library/react-native';

import { botaoProps } from '@/components/molecules/BotaoLoginGoogle.test.helpers';
import { botaoAppleProps } from '@/components/molecules/BotaoLoginApple.test.helpers';

import { HomeHero } from './HomeHero';

jest.mock('@/components/molecules/BotaoLoginGoogle', () => {
  const { BotaoLoginGoogleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginGoogle.test.helpers',
  );
  return { BotaoLoginGoogle: BotaoLoginGoogleDeTeste };
});

jest.mock('@/components/molecules/BotaoLoginApple', () => {
  const { BotaoLoginAppleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginApple.test.helpers',
  );
  return { BotaoLoginApple: BotaoLoginAppleDeTeste };
});

function renderHero(overrides: Partial<Parameters<typeof HomeHero>[0]> = {}) {
  return render(
    <HomeHero
      onAutenticadoGoogle={jest.fn()}
      onCadastroPendenteGoogle={jest.fn()}
      onAutenticadoApple={jest.fn()}
      onCadastroPendenteApple={jest.fn()}
      onEntrarComoProfessor={jest.fn()}
      onEntrarComoAluno={jest.fn()}
      onLogin={jest.fn()}
      {...overrides}
    />,
  );
}

describe('HomeHero', () => {
  it('calls onEntrarComoProfessor when the cadastro Professor CTA is pressed', async () => {
    const onEntrarComoProfessor = jest.fn();
    await renderHero({ onEntrarComoProfessor });

    await fireEvent.press(screen.getByText('Cadastrar como Professor'));

    expect(onEntrarComoProfessor).toHaveBeenCalled();
  });

  it('calls onEntrarComoAluno when the cadastro Aluno CTA is pressed', async () => {
    const onEntrarComoAluno = jest.fn();
    await renderHero({ onEntrarComoAluno });

    await fireEvent.press(screen.getByText('Cadastrar como Aluno'));

    expect(onEntrarComoAluno).toHaveBeenCalled();
  });

  it('calls onLogin when the "Entrar com código ou e-mail" link is pressed', async () => {
    const onLogin = jest.fn();
    await renderHero({ onLogin });

    await fireEvent.press(screen.getByText('Entrar com código ou e-mail'));

    expect(onLogin).toHaveBeenCalled();
  });

  it('repassa onAutenticadoGoogle/onCadastroPendenteGoogle para o BotaoLoginGoogle (issue #114)', async () => {
    const onAutenticadoGoogle = jest.fn();
    const onCadastroPendenteGoogle = jest.fn();
    await renderHero({ onAutenticadoGoogle, onCadastroPendenteGoogle });

    botaoProps.onAutenticado({ token: 't', nome: 'Ana', papeis: ['Professor'] });
    expect(onAutenticadoGoogle).toHaveBeenCalledWith({ token: 't', nome: 'Ana', papeis: ['Professor'] });

    botaoProps.onCadastroPendente('novo@exemplo.com');
    expect(onCadastroPendenteGoogle).toHaveBeenCalledWith('novo@exemplo.com');
  });

  it('repassa onAutenticadoApple/onCadastroPendenteApple para o BotaoLoginApple (issue #212)', async () => {
    const onAutenticadoApple = jest.fn();
    const onCadastroPendenteApple = jest.fn();
    await renderHero({ onAutenticadoApple, onCadastroPendenteApple });

    botaoAppleProps.onAutenticado({ token: 't-apple', nome: 'Ana', papeis: ['Professor'] });
    expect(onAutenticadoApple).toHaveBeenCalledWith({ token: 't-apple', nome: 'Ana', papeis: ['Professor'] });

    botaoAppleProps.onCadastroPendente('novo.apple@gmail.com');
    expect(onCadastroPendenteApple).toHaveBeenCalledWith('novo.apple@gmail.com');
  });

  it('usa rótulos que comunicam a ação de cadastro, não "sou X" (issue #112)', async () => {
    await renderHero();

    expect(screen.getByText('Cadastrar como Professor')).toBeTruthy();
    expect(screen.getByText('Cadastrar como Aluno')).toBeTruthy();
    expect(screen.queryByText('sou Professor')).toBeNull();
    expect(screen.queryByText('sou Aluno')).toBeNull();
  });

  it('shows the erro message when given, near the Google button', async () => {
    await renderHero({ erro: 'Não foi possível concluir o login neste dispositivo. Tente novamente.' });

    expect(
      screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
    ).toBeTruthy();
  });

  it('não mostra mensagem de erro quando erro não é passado', async () => {
    await renderHero();

    expect(screen.queryByText(/Não foi possível/)).toBeNull();
  });

  it('mostra "Entrar com Google" antes dos CTAs de cadastro, com peso visual maior (issue #111)', async () => {
    await renderHero();

    const entrarGoogle = screen.getByText('Entrar com Google');
    const cadastrarProfessor = screen.getByRole('button', { name: 'Cadastrar como Professor' });
    const cadastrarAluno = screen.getByRole('button', { name: 'Cadastrar como Aluno' });

    expect(entrarGoogle).toBeTruthy();
    // Cadastro é variante secundária (sem preenchimento) — Google continua
    // sendo o único CTA cheio/preenchido da tela.
    expect(cadastrarProfessor.props.className).toContain('bg-transparent');
    expect(cadastrarAluno.props.className).toContain('bg-transparent');
  });
});
