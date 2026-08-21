import { render, screen } from '@testing-library/react-native';

import { IconeGoogle } from './IconeGoogle';

describe('IconeGoogle', () => {
  it('renders an image with a data URI source', async () => {
    await render(<IconeGoogle />);

    const imagem = screen.getByTestId('icone-google', { includeHiddenElements: true });
    expect(imagem.props.source.uri).toMatch(/^data:image\/png;base64,/);
  });

  it('defaults to 20px and accepts a custom tamanho', async () => {
    const { rerender } = await render(<IconeGoogle />);

    expect(screen.getByTestId('icone-google', { includeHiddenElements: true })).toHaveStyle({ width: 20, height: 20 });

    await rerender(<IconeGoogle tamanho={32} />);
    expect(screen.getByTestId('icone-google', { includeHiddenElements: true })).toHaveStyle({ width: 32, height: 32 });
  });
});
