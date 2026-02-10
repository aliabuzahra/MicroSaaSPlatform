import { useState } from 'react'
import './App.css'
import { Button } from '@saas/ui'
import { LoginForm } from '@saas/feature-identity'
import { TenantList } from '@saas/feature-tenant'

function App() {
  const [showLogin, setShowLogin] = useState(true)

  return (
    <div style={{ padding: 20 }}>
      <h1>Unified Micro-SaaS Platform</h1>
      <p>Modular Frontend Composition</p>

      <div style={{ margin: '20px 0' }}>
        <Button onClick={() => setShowLogin(!showLogin)}>
          Toggle View
        </Button>
      </div>

      {showLogin ? <LoginForm /> : <TenantList />}
    </div>
  )
}

export default App
