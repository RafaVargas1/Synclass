import { render } from '@testing-library/react-native';

import { TituloDaAba } from './TituloDaAba';

const mockHead = jest.fn();
jest.mock('expo-router/head', () => ({
  __esModule: true,
  default: (props: { children: React.ReactElement }) => {
    mockHead(props);
    return null;
  },
}));

describe('TituloDaAba', () => {
  beforeEach(() => mockHead.mockReset());

  it('renderiza <title> prefixado por "Synclass - "', async () => {
    await render(<TituloDaAba titulo="Valor devido" />);

    const children = mockHead.mock.calls[0][0].children;
    expect(children.type).toBe('title');
    expect(children.props.children).toBe('Synclass - Valor devido');
  });

  it('não duplica o prefixo se o título já começar com Synclass', async () => {
    await render(<TituloDaAba titulo="Synclass - Início" />);

    const children = mockHead.mock.calls[0][0].children;
    expect(children.props.children).toBe('Synclass - Início');
  });
});
