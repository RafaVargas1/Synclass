import { render, screen } from '@testing-library/react-native';
import { X } from 'phosphor-react-native';

import { IconeAcaoAcessivel } from '@/components/atoms/IconeAcaoAcessivel';

describe('IconeAcaoAcessivel (issue #202)', () => {
  it('renders the icon hidden from the accessibility tree', async () => {
    await render(<IconeAcaoAcessivel Icone={X} cor="#DC2626" testID="icone-acao-teste" />);

    const container = screen.getByTestId('icone-acao-teste', { includeHiddenElements: true });
    expect(container).toBeTruthy();
    expect(container.props.accessibilityElementsHidden).toBe(true);
    expect(container.props.importantForAccessibility).toBe('no');
  });
});
