import './App.css';
import { NavBar } from './components/NavBar'
import { Routes, Route } from 'react-router-dom'
import { Characters } from "./components/Characters";
import { Dashboard } from "./pages/Dashboard";
import {Modal} from "./components/Modal";

function App() {
  return (
    <>
      <Modal />
      <div className="App">

        <NavBar />

        <Routes>
          <Route path="/" element={ <Dashboard /> } />
          <Route path="/characters" element={ <Characters /> } />
        </Routes>
      </div>
    </>
  );
}

export default App;
