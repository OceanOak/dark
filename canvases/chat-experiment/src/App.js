import './App.css';
import Chat from './components/Chat';
import DarkPrompt from './components/DarkPrompt';
import Prompt from './components/Prompt';

function App() {
  return (
    <div className="App h-screen">
      <header className="App-header">
        <a href="/">
          <img className="h-14 m-4" src="https://darklang.com/img/wordmark-dark-transparent.png" alt="Darklang logo"/>
        </a>
      </header>
      <DarkPrompt/>
      <Chat />
      <Prompt/>

    </div>
  );
}

export default App;
