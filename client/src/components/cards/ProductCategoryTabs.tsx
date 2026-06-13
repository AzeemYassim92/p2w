export type ProductCategory = {
  id: string;
  label: string;
};

type Props = {
  categories: ProductCategory[];
  activeCategory: string;
  onChange: (categoryId: string) => void;
};

export function ProductCategoryTabs({ categories, activeCategory, onChange }: Props) {
  return (
    <div className="category-tabs" aria-label="Product categories">
      {categories.map((category) => (
        <button
          className={category.id === activeCategory ? 'active' : ''}
          key={category.id}
          onClick={() => onChange(category.id)}
        >
          {category.label}
        </button>
      ))}
    </div>
  );
}
