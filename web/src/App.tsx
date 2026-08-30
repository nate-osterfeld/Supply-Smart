import { useState, useEffect } from 'react';
import './App.css';

interface SupplyItem {
  id: number;
  name: string;
  sku: string;
  quantity: number;
}

function App() {
  const [supplies, setSupplies] = useState<SupplyItem[]>([]);
  const [name, setName] = useState('');
  const [sku, setSku] = useState('');
  const [quantity, setQuantity] = useState(0);

  // Fetch supplies from the backend API on load
  const fetchSupplies = async () => {
    try {
      const response = await fetch('/api/supplies');
      if (response.ok) {
        const data = await response.json();
        setSupplies(data);
      }
    } catch (error) {
      console.error('Error fetching supplies:', error);
    }
  };

  useEffect(() => {
    fetchSupplies();
  }, []);

  // Create a new supply item via POST request
  const addSupply = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!name.trim() || !sku.trim()) return;
    
    try {
      const response = await fetch('/api/supplies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, sku, quantity: Number(quantity) }),
      });

      if (response.ok) {
        setName('');
        setSku('');
        setQuantity(0);
        fetchSupplies(); // Refresh list
      }
    } catch (error) {
      console.error('Error creating supply item:', error);
    }
  };

  return (
    <div style={{ maxWidth: '600px', margin: '40px auto', fontFamily: 'Arial' }}>
      <h1>SupplySmart Inventory</h1>

      <form onSubmit={addSupply} style={{ marginBottom: '20px', display: 'flex', gap: '10px' }}>
        <input
          type="text"
          placeholder="Item Name..."
          value={name}
          onChange={(e) => setName(e.target.value)}
          style={{ padding: '8px', flex: 2 }}
        />
        <input
          type="text"
          placeholder="SKU..."
          value={sku}
          onChange={(e) => setSku(e.target.value)}
          style={{ padding: '8px', flex: 1 }}
        />
        <input
          type="number"
          placeholder="Qty"
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
          style={{ padding: '8px', width: '70px' }}
        />
        <button type="submit" style={{ padding: '8px 16px' }}>Add Item</button>
      </form>

      <ul style={{ listStyleType: 'none', padding: 0 }}>
        {supplies.map((item) => (
          <li key={item.id} style={{ marginBottom: '8px', padding: '8px', background: '#f4f4f4', borderRadius: '4px', display: 'flex', justifyContent: 'space-between' }}>
            <span><strong>{item.name}</strong> (SKU: {item.sku})</span>
            <span>Qty: {item.quantity}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default App;