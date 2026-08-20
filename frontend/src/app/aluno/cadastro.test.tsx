import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cadastrarAluno, verificarContatoAluno } from '@/lib/api/alunos';

import CadastroAlunoScreen from './cadastro';

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/alunos', () => ({
  cadastrarAluno: jest.fn(),
  verificarContatoAluno: jest.fn(),
}));

const cadastrarAlunoMock = cadastrarAluno as jest.Mock;
const verificarContatoAlunoMock = verificarContatoAluno as jest.Mock;

describe('CadastroAlunoScreen', () => {
  beforeEach(() => {
    cadastrarAlunoMock.mockReset();
    verificarContatoAlunoMock.mockReset();
    verificarContatoAlunoMock.mockResolvedValue({ identidadeExistente: false, nome: null });
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    cadastrarAlunoMock.mockResolvedValue({ sucesso: true, nome: 'João Souza' });
    await render(<CadastroAlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Souza');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'joao@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
    expect(screen.getByText('Seu cadastro como Aluno foi realizado com sucesso.')).toBeTruthy();
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarAlunoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O contato já está cadastrado como Aluno.',
    });
    await render(<CadastroAlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Souza');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'joao@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('O contato já está cadastrado como Aluno.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('locks the Nome field with the existing nome when the contato already has an identidade', async () => {
    verificarContatoAlunoMock.mockResolvedValue({
      identidadeExistente: true,
      nome: 'João Souza',
    });
    await render(<CadastroAlunoScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'joao@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(screen.getByDisplayValue('João Souza')).toBeTruthy());
    expect(screen.getByDisplayValue('João Souza').props.editable).toBe(false);
    expect(
      screen.getByText('Contato já cadastrado. Para corrigir o nome, edite pelo perfil depois de logado.'),
    ).toBeTruthy();
  });

  it('keeps the Nome field editable when the contato is new', async () => {
    await render(<CadastroAlunoScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'novo@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(verificarContatoAlunoMock).toHaveBeenCalledWith('novo@exemplo.com'));
    expect(screen.getByPlaceholderText('Seu nome completo').props.editable).not.toBe(false);
  });

  it('shows a client-side error and blocks the submit when the contato is neither an e-mail nor a phone', async () => {
    await render(<CadastroAlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Souza');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), 'contato invalido');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Informe um e-mail ou telefone válido.')).toBeTruthy(),
    );
    expect(cadastrarAlunoMock).not.toHaveBeenCalled();
  });
});
