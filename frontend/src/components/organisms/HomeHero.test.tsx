import { fireEvent, render, screen } from '@testing-library/react-native';

import { HomeHero } from './HomeHero';

describe('HomeHero', () => {
  it('calls onEntrarComoProfessor when the "sou Professor" CTA is pressed', async () => {
    const onEntrarComoProfessor = jest.fn();
    await render(
      <HomeHero
        onEntrarComoProfessor={onEntrarComoProfessor}
        onEntrarComoAluno={jest.fn()}
        onLogin={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByText('sou Professor'));

    expect(onEntrarComoProfessor).toHaveBeenCalled();
  });

  it('calls onEntrarComoAluno when the "sou Aluno" CTA is pressed', async () => {
    const onEntrarComoAluno = jest.fn();
    await render(
      <HomeHero
        onEntrarComoProfessor={jest.fn()}
        onEntrarComoAluno={onEntrarComoAluno}
        onLogin={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByText('sou Aluno'));

    expect(onEntrarComoAluno).toHaveBeenCalled();
  });

  it('calls onLogin when the entrar link is pressed', async () => {
    const onLogin = jest.fn();
    await render(
      <HomeHero
        onEntrarComoProfessor={jest.fn()}
        onEntrarComoAluno={jest.fn()}
        onLogin={onLogin}
      />,
    );

    await fireEvent.press(screen.getByText('Já tenho conta, entrar'));

    expect(onLogin).toHaveBeenCalled();
  });

  it('não usa textos ambíguos cobrindo os dois papéis num único botão', async () => {
    await render(
      <HomeHero
        onEntrarComoProfessor={jest.fn()}
        onEntrarComoAluno={jest.fn()}
        onLogin={jest.fn()}
      />,
    );

    expect(screen.queryByText('Cadastrar como Professor')).toBeNull();
    expect(screen.queryByText(/Cadastrar como/)).toBeNull();
    expect(screen.getByText('sou Professor')).toBeTruthy();
    expect(screen.getByText('sou Aluno')).toBeTruthy();
  });
});
